using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Undead : AnimalNPC, ILimitedSpawner, IDamageable
{
	public float MaxHealth => 80f;

	private float CurrentSpeed { get; set; }

	private float WalkSpeed => 60f;
	private float RunSpeed => 80f;

	private TimeUntil NextFindTarget { get; set; }
	private ForsakenPlayer Target { get; set; }

	public override string GetDisplayName()
	{
		return "Undead";
	}

	public override bool ShouldWander()
	{
		return !Target.IsValid();
	}

	public override float GetIdleDuration()
	{
		return Game.Random.Float( 8f, 16f );
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

	protected virtual void OnLoseTarget( ForsakenPlayer target )
	{
		MoveToLocation( target.Position );
		NextWanderTime = GetIdleDuration();
	}

	protected virtual void OnNewTarget( ForsakenPlayer target )
	{
		MoveToLocation( target.Position );
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
			if ( NextFindTarget )
			{
				var target = FindInSphere( Position, 2048f )
					.OfType<ForsakenPlayer>()
					.Where( CanSeeTarget )
					.OrderBy( p => p.Position.Distance( Position ) )
					.FirstOrDefault();

				if ( target.IsValid() )
				{
					if ( Target != target )
						OnNewTarget( target );
				}
				else if ( Target.IsValid() )
				{
					OnLoseTarget( Target );
				}

				NextFindTarget = 1f;
				Target = target;
			}
		}

		base.ServerTick();
	}

	protected override Vector3 GetWishDirection()
	{
		if ( Target.IsValid() && CanSeeTarget( Target ) )
		{
			return (Target.Position - Position).Normal;
		}

		return base.GetWishDirection();
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
			CurrentSpeed = CurrentSpeed.LerpTo( targetSpeed, Time.Delta * 60f );

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
			.Ignore( this )
			.Run();

		return trace.Entity == target;
	}
}
