using Sandbox;

namespace Facepunch.Forsaken;

[Library( "weapon_pump_shotgun" )]
public partial class PumpShotgun : ProjectileWeapon<CrossbowBoltProjectile>
{
	public override string ImpactEffect => GetImpactEffect();
	public override string TrailEffect => GetTrailEffect();
	public override int ViewModelMaterialGroup => 1;
	public override string MuzzleFlashEffect => "particles/pistol_muzzleflash.vpcf";
	public override string HitSound => null;
	public override DamageFlags DamageType => DamageFlags.Bullet;
	public override float PrimaryRate => 1f;
	public override float SecondaryRate => 1f;
	public override float Speed => 1500f;
	public override int HoldType => 3;
	public override float Gravity => 6f;
	public override float InheritVelocity => 0f;
	public override string ReloadSoundName => "rust_smg.reload";
	public override string ProjectileModel => null;
	public override int ProjectileCount => 8;
	public override float ReloadTime => 1f;
	public override float ProjectileLifeTime => 4f;
	public override float Spread => 0.6f;

	public override void AttackPrimary()
	{
		if ( !TakeAmmo( 1 ) )
		{
			PlaySound( "pistol.dryfire" );
			return;
		}

		PlayAttackAnimation();
		ShootEffects();
		PlaySound( $"shotgun1_shoot" );
		ApplyRecoil();

		base.AttackPrimary();
	}

	protected override void OnReloadFinish()
	{
		IsReloading = false;
		TimeSinceReload = 0f;
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( AmmoClip >= ClipSize )
			return;

		if ( Owner is not ForsakenPlayer player )
			return;

		if ( !UnlimitedAmmo )
		{
			var ammo = player.TakeAmmo( WeaponItem.AmmoType, 1 );

			if ( ammo == 0 )
				return;

			AmmoClip += 1;
		}
		else
		{
			AmmoClip += 1;
		}

		if ( AmmoClip < ClipSize )
		{
			Reload();
		}
	}

	protected override void ShootEffects()
	{
		var position = GetMuzzlePosition();

		if ( position.HasValue )
			CreateLightSource( position.Value, Color.White, 300f, 0.1f, Time.Delta );

		base.ShootEffects();
	}

	protected override void OnProjectileHit( CrossbowBoltProjectile projectile, TraceResult trace )
	{
		if ( IsServer && trace.Entity is ForsakenPlayer victim )
		{
			var info = new DamageInfo()
				.WithAttacker( Owner )
				.WithWeapon( this )
				.WithPosition( trace.EndPosition )
				.WithForce( projectile.Velocity * 0.1f )
				.WithFlag( DamageType )
				.UsingTraceResult( trace );

			info.Damage = GetDamageFalloff( projectile.StartPosition.Distance( victim.Position ), WeaponItem.Damage );

			victim.TakeDamage( info );
			
			using ( Prediction.Off() )
			{
				victim.PlaySound( "melee.hitflesh" );
			}
		}
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
