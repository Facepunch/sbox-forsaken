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
	private float AttackRate => 1f;
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
		if ( LifeState == LifeState.Alive )
		{
			if ( ( !Target.IsValid() || Position.Distance( Target.Position ) > 2048f ) && NextFindTarget )
			{
				var target = FindInSphere( Position, 2048f )
					.OfType<ForsakenPlayer>()
					.Where( CanSeeTarget )
					.OrderBy( p => p.Position.Distance( Position ) )
					.FirstOrDefault();

				if ( target.IsValid() )
				{
					State = MovementState.Moving;
					Path = null;
				}

				NextFindTarget = 8f;
				Target = target;
			}

			if ( Target.IsValid() )
				IsTargetVisible = CanSeeTarget( Target );
			else
				IsTargetVisible = false;

			if ( Target.IsValid() && Position.Distance( Target.Position ) <= AttackRadius )
			{
				if ( CanAttack() )
				{
					TimeSinceLastAttack = 0f;
					SetAnimParameter( "attack", true );
				}
			}

			if ( Target.IsValid() )
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

		if ( !Target.IsValid() && NextWanderTime )
		{
			NextWanderTime = Game.Random.Float( 4f, 8f );
			Wander.Regenerate();
		}

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

		var acceleration = Avoidance.GetSteering();

		if ( HasValidPath() )
		{
			var direction = (GetPathTarget() - Position).Normal;
			acceleration = direction * GetMoveSpeed();
		}
		else if ( Target.IsValid() )
		{
			if ( Position.Distance( Target.Position ) > TargetRange )
				acceleration += Steering.Seek( Target.Position );
		}
		else
		{
			acceleration += Wander.GetSteering();
		}

		if ( !acceleration.IsNearZeroLength )
		{
			Steering.Steer( acceleration );
		}

		var nearbyUndead = FindInSphere( Position, 100f )
			.OfType<Undead>()
			.ToArray();

		/*
		var moveSpeed = GetMoveSpeed();

		var flocker = new Flocker();
		flocker.Setup( this, nearbyUndead, Position, moveSpeed, 60f );
		flocker.Flock( Position + direction * moveSpeed );

		var steerDirection = flocker.Force.WithZ( 0f );
		//Velocity = Accelerate( Velocity, steerDirection.Normal, moveSpeed, 0f, 8f );
		*/
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
