using Sandbox;

namespace Facepunch.Forsaken;

public partial class Deer : AnimalNPC, ILimitedSpawner, IDamageable
{
	public TimeSince LastDamageTime { get; set; }

	public float MaxHealth => 80f;

	public override string GetDisplayName()
	{
		return "Deer";
	}

	public override bool ShouldWander()
	{
		return true;
	}

	public override float GetMoveSpeed()
	{
		return LastDamageTime < 10f ? 400f : 120f;
	}

	public override void Spawn()
	{
		SetModel( "models/deer/deer.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Scale = Game.Random.Float( 0.9f, 1.1f );

		base.Spawn();
	}

	public override void OnKilled()
	{
		LifeState = LifeState.Dead;
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

		LastDamageTime = 0f;

		if ( TryGetNavMeshPosition( 2000f, 4000f, out var targetPosition ) )
		{
			MoveToLocation( targetPosition );
			NextWanderTime = 10f;
		}

		base.TakeDamage( info );
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
			var speed = Velocity.Length.Remap( 0f, 400f, 0f, 1f );
			SetAnimParameter( "dead", false );
			SetAnimParameter( "speed", speed );
		}
	}
}
