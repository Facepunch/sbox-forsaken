using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Undead : Animal, ILimitedSpawner, IDamageable
{
	public float MaxHealth => 80f;

	public bool IsTargetVisible { get; private set; }

	private bool IsAttackingStructure { get; set; }
	private Vector3 StructurePosition { get; set; }
	private float CurrentSpeed { get; set; }
	private float TargetRange => 60f;
	private float AttackRadius => 60f;
	private float AttackRate => 0.5f;
	private float WalkSpeed => 60f;
	private float RunSpeed => 80f;

	private TimeSince TimeSinceLastAttack { get; set; }
	private TimeUntil NextFindTarget { get; set; }
	private ForsakenPlayer Target { get; set; }

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

	protected override void ServerTick()
	{
		IsAttackingStructure = false;

		if ( LifeState == LifeState.Alive )
		{
			if ( ( !Target.IsValid() || Position.Distance( Target.Position ) > 2048f || Target.LifeState == LifeState.Dead) && NextFindTarget )
			{
				var target = FindInSphere( Position, 1024f )
					.OfType<ForsakenPlayer>()
					.Where( CanSeeTarget )
					.OrderBy( p => p.Position.Distance( Position ) )
					.FirstOrDefault();

				if ( target.IsValid() )
				{
					State = MovementState.Moving;
				}

				NextFindTarget = 1f;
				Target = target;
				Path = null;
			}

			if ( Target.IsValid() )
				IsTargetVisible = CanSeeTarget( Target );
			else
				IsTargetVisible = false;

			var distanceToTarget = Target.IsValid() ? Position.Distance( Target.Position ) : 0f;

			if ( Target.IsValid() && !HasValidPath() && distanceToTarget < 512f )
			{
				var eyePosition = Position + Vector3.Up * 64f;
				var directionToTarget = (Target.Position - Position).Normal;
				var trace = Trace.Ray( eyePosition, eyePosition + directionToTarget * 512f )
					.EntitiesOnly()
					.WithAnyTags( "wall", "door" )
					.Ignore( this )
					.Run();

				if ( trace.Hit && trace.Entity.IsValid() )
				{
					StructurePosition = trace.EndPosition;
					IsAttackingStructure = true;
					Path?.Clear();
				}
			}

			if ( !IsAttackingStructure )
			{
				if ( Target.IsValid() && Position.Distance( Target.Position ) <= AttackRadius )
				{
					if ( CanAttack() )
					{
						MeleeStrike();
					}
				}
			}
			else if ( Position.Distance( StructurePosition ) <= AttackRadius * 2f )
			{
				if ( CanAttack() )
				{
					MeleeStrike();
				}
			}

			if ( Target.IsValid() && !IsAttackingStructure )
			{
				if ( !IsTargetVisible && !HasValidPath() )
				{
					MoveToLocation( Target.Position );
				}
				else if ( IsTargetVisible )
				{
					Path = null;
				}
			}
		}

		base.ServerTick();
	}

	protected override void UpdateRotation()
	{
		if ( IsAttackingStructure )
		{
			RotateOverTime( (StructurePosition - Position).Normal );
			return;
		}

		if ( Target.IsValid() && IsTargetVisible )
		{
			RotateOverTime( Target );
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

		if ( !IsAttackingStructure )
		{
			if ( HasValidPath() )
			{
				var direction = (GetPathTarget() - Position).Normal;
				acceleration += direction * GetMoveSpeed();

				if ( Debug )
					DebugOverlay.Sphere( Position, 16f, Color.Green );
			}
			else if ( Target.IsValid() )
			{
				if ( Target.Position.Distance( Position ) > TargetRange )
					acceleration += Steering.Seek( Target.Position, 60f );
			}
			else
			{
				acceleration += Wander.GetSteering();
			}
		}
		else
		{
			if ( StructurePosition.Distance( Position ) > AttackRadius )
			{
				var direction = (StructurePosition - Position).Normal.WithZ( 0f );
				acceleration = direction * GetMoveSpeed();
			}
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

	private bool CanSeeTarget( Entity target )
	{
		var eyePosition = Position + Vector3.Up * 16f;
		var trace = Trace.Ray( eyePosition, target.Position + Vector3.Up * 16f )
			.WorldAndEntities()
			.WithoutTags( "passplayers", "trigger", "npc" )
			.WithAnyTags( "solid", "world", "player" )
			.Size( 8f )
			.Ignore( this )
			.Run();

		return trace.Entity == target;
	}
}
