using Sandbox;
using System;
using System.Collections;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Undead : Animal, ILimitedSpawner, IDamageable
{
	private enum UndeadPose
	{
		Default,
		Rising
	}

	public float MaxHealth => 80f;

	public bool IsTargetVisible { get; private set; }

	private float CurrentSpeed { get; set; }
	private float TargetRange => 60f;
	private float AttackRadius => 60f;
	private float AttackRate => 0.5f;
	private float WalkSpeed => 60f;
	private float RunSpeed => 80f;

	private TimeSince TimeSinceLastAttack { get; set; }
	private TimeUntil NextFindTarget { get; set; }
	private float TimeSinceRisen { get; set; }
	private IDamageable Target { get; set; }
	private UndeadPose Pose { get; set; }

	public void RiseFromGround()
	{
		TimeSinceRisen = 0f;
		Rotation = new Angles( 0f, Game.Random.Float( 0f, 360f ), 0f ).ToRotation();
		State = MovementState.Idle;
		Pose = UndeadPose.Rising;
	}

	public virtual void Despawn()
	{
		if ( LifeState == LifeState.Dead )
			return;

		var damage = new DamageInfo()
			.WithDamage( Game.Random.Float( 20f, 30f ) )
			.WithTag( "burn" )
			.WithPosition( Position );

		TakeDamage( damage );
	}

	public override string GetDisplayName()
	{
		return "Undead";
	}

	public override float GetMoveSpeed()
	{
		if ( Target.IsValid() )
			return RunSpeed;

		return WalkSpeed;
	}

	public override void Spawn()
	{
		SetModel( "models/undead/undead.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		EnableSolidCollisions = false;
		NextFindTarget = 0f;
		LifeState = LifeState.Alive;
		Health = MaxHealth;
		Scale = Game.Random.Float( 0.9f, 1.1f );

		base.Spawn();
	}

	public override void OnKilled()
	{
		LifeState = LifeState.Dead;
		DeleteAsync( 5f );
	}

	protected float GetDistanceToTarget()
	{
		if ( Target.IsValid() )
			return Position.Distance( Target.WorldSpaceBounds.ClosestPoint( Position ) );
		else
			return 0f;
	}

	protected virtual IOrderedEnumerable<ForsakenPlayer> GetPlayerTargets()
	{
		return FindInSphere( Position, 1024f )
			.OfType<ForsakenPlayer>()
			.Where( CanSeeTarget )
			.OrderBy( p => p.Position.Distance( Position ) );
	}

	protected virtual IOrderedEnumerable<Structure> GetStructureTargets()
	{
		return FindInSphere( Position, 1024f )
			.OfType<Structure>()
			.Where( s => s.Tags.Has( "wall" ) || s.Tags.Has( "door" ) )
			.OrderBy( p => p.Position.Distance( Position ) );
	}

	protected virtual bool ShouldAttackStructures()
	{
		return FindInSphere( Position, 1024f ).OfType<ForsakenPlayer>().Any();
	}

	protected virtual void MeleeStrike()
	{
		TimeSinceLastAttack = 0f;
		SetAnimParameter( "attack", true );
		PlaySound( "melee.swing" );
	}

	protected virtual bool CanAttack()
	{
		return TimeSinceLastAttack > (1f / AttackRate);
	}

	protected virtual void UpdateTarget()
	{
		var playerTarget = GetPlayerTargets().FirstOrDefault();

		Target = null;

		if ( playerTarget.IsValid() )
		{
			Target = playerTarget;
			State = MovementState.Moving;
			return;
		}

		if ( !ShouldAttackStructures() )
			return;

		var structureTarget = GetStructureTargets().FirstOrDefault();

		if ( structureTarget.IsValid() )
		{
			State = MovementState.Moving;
			Target = structureTarget;
		}
	}

	protected virtual bool IsTargetStale()
	{
		if ( !Target.IsValid() || Position.Distance( Target.Position ) > 2048f || Target.LifeState == LifeState.Dead )
			return true;

		return false;
	}

	public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
	{
		if ( name == "attack" && Game.IsServer )
		{
			var eyePosition = Position + Vector3.Up * 64f;
			var trace = Trace.Ray( eyePosition, eyePosition + Rotation.Forward * AttackRadius )
				.WorldAndEntities()
				.WithAnyTags( "solid", "player", "wall", "door" )
				.Ignore( this )
				.Size( 4f )
				.Run();

			if ( trace.Entity.IsValid() && trace.Entity is IDamageable damageable )
			{
				var damage = new DamageInfo()
					.WithAttacker( this )
					.WithWeapon( this )
					.WithPosition( trace.EndPosition )
					.WithDamage( 10f )
					.WithForce( Rotation.Forward * 100f * 1f )
					.WithTag( "melee" );

				damageable.TakeDamage( damage );
			}
		}

		base.OnAnimEventGeneric( name, intData, floatData, vectorData, stringData );
	}

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState == LifeState.Dead ) return;

		if ( info.Attacker is ForsakenPlayer attacker )
		{
			if ( info.HasTag( "bullet" ) )
			{
				if ( info.Hitbox.HasTag( "head" ) )
				{
					Sound.FromScreen( To.Single( attacker ), "hitmarker.headshot" );
					info.Damage *= 2f;
				}
				else
				{
					Sound.FromScreen( To.Single( attacker ), "hitmarker.hit" );
				}
			}
		}

		if ( info.HasTag( "bullet" ) )
		{
			using ( Prediction.Off() )
			{
				var particles = Particles.Create( "particles/gameplay/player/taken_damage/taken_damage.vpcf", info.Position );
				particles.SetForward( 0, info.Force.Normal );

				PlaySound( "melee.hitflesh" );
			}
		}

		base.TakeDamage( info );
	}

	protected override void UpdateLogic()
	{
		base.UpdateLogic();

		if ( LifeState == LifeState.Dead )
			return;

		if ( Pose == UndeadPose.Rising )
		{
			TimeSinceRisen += Time.Delta;

			if ( TimeSinceRisen >= 4f )
				Pose = UndeadPose.Default;

			EnableDrawing = TimeSinceRisen >= 0.25f;
			return;
		}

		if ( NextFindTarget && IsTargetStale() )
		{
			UpdateTarget();
		}

		if ( Target.IsValid() )
		{
			IsTargetVisible = CanSeeTarget( Target );

			if ( IsTargetVisible )
			{
				ClearPath();
			}
		}
		else
		{
			IsTargetVisible = false;
		}

		if ( NextFindTarget )
		{
			if ( Target.IsValid() && !IsTargetVisible )
			{
				if ( !TryFindPath( Target.Position, Target is ForsakenPlayer ) )
				{
					Target = null;
				}
			}

			if ( Target.IsValid() && Target is not ForsakenPlayer )
			{
				var hasAnyPlayerTarget = GetPlayerTargets().Any();

				if ( hasAnyPlayerTarget )
				{
					Target = null;
				}
			}

			NextFindTarget = 1f;
		}

		if ( Target.IsValid() && GetDistanceToTarget() <= AttackRadius )
		{
			if ( CanAttack() )
			{
				MeleeStrike();
			}
		}
	}

	protected override bool CanChangeState()
	{
		if ( Pose == UndeadPose.Rising )
			return false;

		return base.CanChangeState();
	}

	protected override void UpdateRotation()
	{
		if ( Pose == UndeadPose.Rising )
		{
			// No need to rotate if we're rising from the ground.
			return;
		}

		if ( HasValidPath() )
		{
			var direction = (GetPathTarget() - Position).Normal;
			RotateOverTime( direction );
			return;
		}

		if ( Target.IsValid() && IsTargetVisible )
		{
			RotateOverTime( (Entity)Target );
			return;
		}

		base.UpdateRotation();
	}

	protected override void HandleBehavior()
	{
		Steering.MaxVelocity = GetMoveSpeed();
		Steering.MaxAcceleration = Steering.MaxVelocity * 0.25f;

		if ( !Target.IsValid() && NextChangeState && CanChangeState() )
		{
			if ( State == MovementState.Idle )
			{
				NextChangeState = Game.Random.Float( 6f, 12f );
				State = MovementState.Moving;
			}
			else
			{
				NextChangeState = Game.Random.Float( 6f, 16f );
				State = MovementState.Idle;
			}
		}
	}

	protected override void UpdateVelocity()
	{
		if ( State == MovementState.Idle )
		{
			Velocity = Vector3.Zero;
			return;
		}

		var nearbyUndead = FindInSphere( Position, 100f ).OfType<Undead>();
		var acceleration = Vector3.Zero;
		var separation = Components.GetOrCreate<SeparationBehavior>();

		if ( HasValidPath() )
		{
			var direction = (GetPathTarget() - Position).Normal;
			acceleration += direction * GetMoveSpeed();

			if ( Debug )
			{
				DebugOverlay.Sphere( Position, 16f, Color.Green );
				DebugOverlay.Text( "PATH", Position );
			}
		}
		else if ( Target.IsValid() && IsTargetVisible )
		{
			acceleration += separation.GetSteering( nearbyUndead ) * 2f;
			acceleration += Avoidance.GetSteering();

			if ( GetDistanceToTarget() > TargetRange )
				acceleration += Steering.Seek( Target.Position, 60f );

			if ( Debug )
				DebugOverlay.Text( "BEAM", Position );
		}
		else if ( !Target.IsValid() )
		{
			acceleration += separation.GetSteering( nearbyUndead ) * 2f;
			acceleration += Wander.GetSteering();
			acceleration += Avoidance.GetSteering();

			if ( Debug )
				DebugOverlay.Text( "WANDER", Position );
		}

		if ( !acceleration.IsNearZeroLength )
		{
			Steering.Steer( acceleration );
		}
	}

	protected override void HandleAnimation()
	{
		if ( LifeState == LifeState.Dead )
		{
			SetAnimParameter( "dead", true );
			SetAnimParameter( "speed", 0f );
		}
		else
		{
			var targetSpeed = Velocity.WithZ( 0f ).Length;
			CurrentSpeed = CurrentSpeed.LerpTo( targetSpeed, Time.Delta * 8f );

			SetAnimParameter( "dead", false );
			SetAnimParameter( "speed", CurrentSpeed );
		}

		SetAnimParameter( "rising", Pose == UndeadPose.Rising && TimeSinceRisen < 4f );
	}

	private bool CanSeeTarget( IDamageable target )
	{
		var eyePosition = Position + Vector3.Up * 32f;
		var targetPosition = target.Position + Vector3.Up * 32f;
		var direction = (targetPosition - eyePosition).Normal;
		var trace = Trace.Ray( eyePosition, eyePosition + direction * 1024f )
			.WorldAndEntities()
			.WithoutTags( "passplayers", "trigger", "npc" )
			.WithAnyTags( "solid", "world", "player", "door", "wall" )
			.Size( 16f )
			.Ignore( this )
			.Run();

		return trace.Entity == target;
	}
}
