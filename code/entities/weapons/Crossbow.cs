using Sandbox;

namespace Facepunch.Forsaken;

[Library( "weapon_crossbow" )]
public partial class Crossbow : ProjectileWeapon<CrossbowBoltProjectile>
{
	public override string ImpactEffect => GetImpactEffect();
	public override string TrailEffect => GetTrailEffect();
	public override int ViewModelMaterialGroup => 1;
	public override string MuzzleFlashEffect => null;
	public override string HitSound => null;
	public override string DamageType => "bullet";
	public override float PrimaryRate => 0.3f;
	public override float SecondaryRate => 1f;
	public override float Speed => 1500f;
	public override float Gravity => 6f;
	public override float InheritVelocity => 0f;
	public override string ReloadSoundName => "crossbow.reload";
	public override string ProjectileModel => null;
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Rifle;
	public override int ClipSize => 1;
	public override float ReloadTime => 2.3f;
	public override float ProjectileLifeTime => 4f;

	public override void AttackPrimary()
	{
		if ( !TakeAmmo( 1 ) )
		{
			PlaySound( "pistol.dryfire" );
			return;
		}

		PlayAttackAnimation();
		ShootEffects();
		PlaySound( $"crossbow.fire" );

		base.AttackPrimary();
	}

	protected override void OnProjectileHit( CrossbowBoltProjectile projectile, TraceResult trace )
	{
		if ( Game.IsServer && trace.Entity is ForsakenPlayer victim )
		{
			var info = new DamageInfo()
				.WithAttacker( Owner )
				.WithWeapon( this )
				.WithPosition( trace.EndPosition )
				.WithForce( projectile.Velocity * 0.1f )
				.WithTag( DamageType )
				.UsingTraceResult( trace );

			info.Damage = GetDamageFalloff( projectile.StartPosition.Distance( victim.Position ), WeaponItem.Damage );

			victim.TakeDamage( info );
			
			using ( Prediction.Off() )
			{
				victim.PlaySound( "melee.hitflesh" );
			}
		}
	}

	protected override Vector3? GetMuzzlePosition()
	{
		return Transform.PointToWorld( LocalPosition );
	}

	private string GetTrailEffect()
	{
		return "particles/weapons/crossbow/crossbow_trail.vpcf";
	}

	private string GetImpactEffect()
	{
		return "particles/weapons/crossbow/crossbow_impact.vpcf";
	}
}
