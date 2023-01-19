﻿using Sandbox;

namespace Facepunch.Forsaken;

[Library( "weapon_pump_shotgun" )]
public partial class PumpShotgun : ProjectileWeapon<CrossbowBoltProjectile>
{
	public override string ImpactEffect => GetImpactEffect();
	public override string TrailEffect => GetTrailEffect();
	public override int ViewModelMaterialGroup => 1;
	public override string MuzzleFlashEffect => "particles/pistol_muzzleflash.vpcf";
	public override string HitSound => null;
	public override string DamageType => "bullet";
	public override float PrimaryRate => 1f;
	public override float SecondaryRate => 1f;
	public override float Speed => 1500f;
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Shotgun;
	public override float Gravity => 6f;
	public override float InheritVelocity => 0f;
	public override string ReloadSoundName => "shotgun_load";
	public override string ProjectileModel => null;
	public override int ProjectileCount => IsSlugAmmo() ? 1 : 8;
	public override float ReloadTime => 1f;
	public override float ProjectileLifeTime => 4f;
	public override float Spread => IsSlugAmmo() ? 0.05f : 1f;

	public override void AttackPrimary()
	{
		if ( !TakeAmmo( 1 ) )
		{
			PlaySound( "gun.dryfire" );
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
		Game.AssertServer();

		IsReloading = false;
		TimeSinceReload = 0f;

		ResetReloading();

		if ( AmmoClip >= ClipSize )
			return;

		if ( Owner is not ForsakenPlayer player )
			return;

		if ( !WeaponItem.IsValid() )
			return;

		if ( !WeaponItem.AmmoDefinition.IsValid() )
			return;

		if ( !UnlimitedAmmo )
		{
			var ammo = player.TakeAmmo( WeaponItem.AmmoDefinition.UniqueId, 1 );
			if ( ammo == 0 ) return;
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
		if ( Game.IsServer && trace.Entity is ForsakenPlayer victim )
		{
			var info = new DamageInfo()
				.WithAttacker( Owner )
				.WithWeapon( this )
				.WithPosition( trace.EndPosition )
				.WithForce( projectile.Velocity * 0.05f )
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

	private bool IsSlugAmmo()
	{
		if ( !WeaponItem.IsValid() )
			return false;

		if ( !WeaponItem.AmmoDefinition.IsValid() )
			return false;

		return WeaponItem.AmmoDefinition.Tags.Contains( "slug" );
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
