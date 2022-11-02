using Sandbox;

namespace Facepunch.Forsaken;

[Library]
public class CrossbowConfig : WeaponConfig
{
	public override string ClassName => "weapon_crossbow";
	public override AmmoType AmmoType => AmmoType.Bolt;
	public override WeaponType Type => WeaponType.Projectile;
	public override int Ammo => 10;
	public override int Damage => 40;
}

[Library( "weapon_crossbow" )]
public partial class Crossbow : ProjectileWeapon<CrossbowBoltProjectile>
{
	public override WeaponConfig Config => new CrossbowConfig();
	public override string ImpactEffect => GetImpactEffect();
	public override string TrailEffect => GetTrailEffect();
	public override int ViewModelMaterialGroup => 1;
	public override string MuzzleFlashEffect => null;
	public override string HitSound => null;
	public override DamageFlags DamageType => DamageFlags.Bullet;
	public override float PrimaryRate => 0.3f;
	public override float SecondaryRate => 1f;
	public override float Speed => 1500f;
	public override float Gravity => 6f;
	public override float InheritVelocity => 0f;
	public override string ReloadSoundName => "crossbow.reload";
	public override string ProjectileModel => null;
	public override int ClipSize => 1;
	public override float ReloadTime => 2.3f;
	public override float ProjectileLifeTime => 4f;

	public override void Spawn()
	{
		base.Spawn();
		SetModel( "weapons/rust_crossbow/rust_crossbow.vmdl" );
	}

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

	public override void SimulateAnimator( PawnAnimator anim )
	{
		anim.SetAnimParameter( "holdtype", 2 );
	}

	protected override void OnProjectileHit( CrossbowBoltProjectile projectile, TraceResult trace )
	{
		if ( IsServer && trace.Entity is Player victim )
		{
			var info = new DamageInfo()
				.WithAttacker( Owner )
				.WithWeapon( this )
				.WithPosition( trace.EndPosition )
				.WithForce( projectile.Velocity * 0.1f )
				.WithFlag( DamageType )
				.UsingTraceResult( trace );

			info.Damage = GetDamageFalloff( projectile.StartPosition.Distance( victim.Position ), Config.Damage * WeaponItem.Tier );

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
