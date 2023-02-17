using Sandbox;

namespace Facepunch.Forsaken;

[Library( "weapon_crossbow" )]
public partial class Crossbow : ProjectileWeapon<CrossbowBoltProjectile>
{
	public override string MuzzleFlashEffect => null;
	public override string ProjectileData => "bolt";
	public override string DamageType => "bullet";
	public override float PrimaryRate => 0.3f;
	public override float SecondaryRate => 1f;
	public override float InheritVelocity => 0f;
	public override string ReloadSoundName => "crossbow.reload";
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Rifle;
	public override int ClipSize => 1;
	public override float ReloadTime => 2.3f;

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

	protected override Vector3? GetMuzzlePosition()
	{
		return Transform.PointToWorld( LocalPosition );
	}
}
