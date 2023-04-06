using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Undead : Animal, ILimitedSpawner, IDamageable
{
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
	private IDamageable Target { get; set; }

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
		Health = MaxHealth;
		Scale = Game.Random.Float( 0.9f, 1.1f );

		base.Spawn();
	}

	public override void OnKilled()
	{
		LifeState = LifeState.Dead;
	}

	protected virtual void MeleeStrike()
	{
		TimeSinceLastAttack = 0f;
		SetAnimParameter( "attack", true );

		var eyePosition = Position + Vector3.Up * 64f;
		var trace = Trace.Ray( eyePosition, eyePosition + Rotation.Forward * AttackRadius * 2f )
			.WorldAndEntities()
			.WithAnyTags( "solid", "player", "wall", "door" )
			.Ignore( this )
			.Run();

		if ( trace.Entity.IsValid() && trace.Entity is IDamageable damageable )
		{
			var damage = new DamageInfo()
				.WithAttacker( this )
				.WithWeapon( this )
				.WithPosition( trace.EndPosition )
				.WithDamage( 10f )
				.WithForce( Rotation.Forward * 100 * 1f )
				.WithTag( "melee" );

			damageable.TakeDamage( damage );
		}
	}

	protected virtual bool CanAttack()
	{
		return TimeSinceLastAttack > (1f / AttackRate);
	}

	public override void OnAnimEventGeneric( string name, int intData, float floatData, Vector3 vectorData, string stringData )
	{
		Log.Info( name );

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
		if ( LifeState == LifeState.Alive )
		{
			if ( ( !Target.IsValid() || Position.Distance( Target.Position ) > 2048f || Target.LifeState == LifeState.Dead) && NextFindTarget )
			{
				var entitiesInSphere = FindInSphere( Position, 1024f );
				var target = entitiesInSphere
					.OfType<ForsakenPlayer>()
					.Where( CanSeeTarget )
					.OrderBy( p => p.Position.Distance( Position ) )
					.FirstOrDefault();

				Target = null;

				if ( target.IsValid() )
				{
					Target = target;
					State = MovementState.Moving;
				}
				else
				{
					if ( entitiesInSphere.OfType<ForsakenPlayer>().Any() )
					{
						var structureTarget = entitiesInSphere
							.OfType<Structure>()
							.Where( s => s.Tags.Has( "wall" ) || s.Tags.Has( "door" ) )
							.OrderBy( p => p.Position.Distance( Position ) )
							.FirstOrDefault();

						if ( structureTarget.IsValid() )
						{
							State = MovementState.Moving;
							Target = structureTarget;
						}
					}
				}
			}

			if ( Target.IsValid() )
				IsTargetVisible = CanSeeTarget( Target );
			else
				IsTargetVisible = false;

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
					var entitiesInSphere = FindInSphere( Position, 1024f );
					var hasPlayerTarget = entitiesInSphere
						.OfType<ForsakenPlayer>()
						.Where( CanSeeTarget )
						.OrderBy( p => p.Position.Distance( Position ) )
						.Any();

					if ( hasPlayerTarget )
					{
						Target = null;
					}
				}

				NextFindTarget = 1f;
			}

			if ( Target.IsValid() && Position.Distance( Target.Position ) <= AttackRadius )
			{
				if ( CanAttack() )
				{
					MeleeStrike();
				}
			}
		}

		base.UpdateLogic();
	}

	protected override void UpdateRotation()
	{
		if ( Target.IsValid() && IsTargetVisible )
		{
			RotateOverTime( (Entity)Target );
			return;
		}

		if ( HasValidPath() )
		{
			var direction = (GetPathTarget() - Position).Normal;
			RotateOverTime( direction );
			return;
		}

		base.UpdateRotation();
	}

	protected override void HandleBehavior()
	{
		Steering.MaxVelocity = GetMoveSpeed();
		Steering.MaxAcceleration = Steering.MaxVelocity * 0.25f;

		if ( !Target.IsValid() && NextChangeState )
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
		var acceleration = Avoidance.GetSteering();
		var separation = Components.GetOrCreate<SeparationBehavior>();

		acceleration += separation.GetSteering( nearbyUndead ) * 2f;

		if ( Target.IsValid() && IsTargetVisible )
		{
			if ( Target.Position.Distance( Position ) > TargetRange )
				acceleration += Steering.Seek( Target.Position, 60f );
		}
		else if ( HasValidPath() )
		{
			var direction = (GetPathTarget() - Position).Normal;
			acceleration += direction * GetMoveSpeed();

			if ( Debug )
				DebugOverlay.Sphere( Position, 16f, Color.Green );
		}
		else
		{
			acceleration += Wander.GetSteering();
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
	}

	private bool CanSeeTarget( IDamageable target )
	{
		var eyePosition = Position + Vector3.Up * 64f;
		var targetPosition = target.Position + Vector3.Up * 32f;
		var direction = (targetPosition - eyePosition).Normal;
		var trace = Trace.Ray( eyePosition, eyePosition + direction * 1024f )
			.WorldAndEntities()
			.WithoutTags( "passplayers", "trigger", "npc" )
			.WithAnyTags( "solid", "world", "player", "door", "wall" )
			.Size( 2f )
			.Ignore( this )
			.Run();

		return trace.Entity == target;
	}
}
