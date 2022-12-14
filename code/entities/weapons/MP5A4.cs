using Sandbox;

namespace Facepunch.Forsaken;

[Library( "weapon_mp5a4" )]
public partial class MP5A4 : ProjectileWeapon<CrossbowBoltProjectile>
{
	public override string ImpactEffect => GetImpactEffect();
	public override string TrailEffect => GetTrailEffect();
	public override int ViewModelMaterialGroup => 1;
	public override string MuzzleFlashEffect => "particles/pistol_muzzleflash.vpcf";
	public override string HitSound => null;
	public override string DamageType => "bullet";
	public override float PrimaryRate => 10f;
	public override float SecondaryRate => 1f;
	public override float Speed => 1500f;
	public override float Gravity => 6f;
	public override float InheritVelocity => 0f;
	public override string ReloadSoundName => "mp5.mag";
	public override string ProjectileModel => null;
	public override float ReloadTime => 2f;
	public override float ProjectileLifeTime => 4f;
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Rifle;

	public override void AttackPrimary()
	{
		if ( !TakeAmmo( 1 ) )
		{
			PlaySound( "gun.dryfire" );
			return;
		}

		PlayAttackAnimation();
		ShootEffects();
		PlaySound( $"smg1_shoot" );
		ApplyRecoil();

		base.AttackPrimary();
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

	private string GetTrailEffect()
	{
		return "particles/weapons/crossbow/crossbow_trail.vpcf";
	}

	private string GetImpactEffect()
	{
		return "particles/weapons/crossbow/crossbow_impact.vpcf";
	}
}
