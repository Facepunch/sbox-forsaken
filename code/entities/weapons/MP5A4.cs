using Sandbox;

namespace Facepunch.Forsaken;

[Library( "weapon_mp5a4" )]
public partial class MP5A4 : ProjectileWeapon<CrossbowBoltProjectile>
{
	public override string MuzzleFlashEffect => "particles/pistol_muzzleflash.vpcf";
	public override string ProjectileData => "bullet";
	public override string DamageType => "bullet";
	public override float PrimaryRate => 10f;
	public override float SecondaryRate => 1f;
	public override float Spread => 0.025f;
	public override float InheritVelocity => 0f;
	public override string ReloadSoundName => "mp5.mag";
	public override float ReloadTime => 2f;
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
}
