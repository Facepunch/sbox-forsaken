using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Undead : AnimalNPC, ILimitedSpawner, IDamageable
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

	protected override Vector3 GetWishDirection()
	{
		Vector3 direction;

		if ( Target.IsValid() && IsTargetVisible )
		{
			if ( Position.Distance( Target.Position ) <= TargetRange )
				direction =  Vector3.Zero;
			else
				direction = (Target.Position - Position).Normal;
		}
		else
		{
			direction = base.GetWishDirection();
		}

		return direction;
	}

	protected override void UpdateRotation( Vector3 direction )
	{
		if ( Target.IsValid() && IsTargetVisible )
		{
			//RotateOverTime( Target );
			return;
		}

		base.UpdateRotation( direction );
	}

	protected override void UpdateVelocity( Vector3 direction )
	{
		if ( !IsTargetVisible )
		{
			base.UpdateVelocity( direction );
			return;
		}

		var nearbyUndead = FindInSphere( Position, 100f )
			.OfType<Undead>()
			.ToArray();

		var moveSpeed = GetMoveSpeed();

		var flocker = new Flocker();
		flocker.Setup( this, nearbyUndead, Position, moveSpeed, 60f );
		flocker.Flock( Position + direction * moveSpeed );

		var steerDirection = flocker.Force.WithZ( 0f );
		//Velocity = Accelerate( Velocity, steerDirection.Normal, moveSpeed, 0f, 8f );
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
			var targetSpeed = Velocity.Length;
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
