using Sandbox;

namespace Facepunch.Forsaken;

public abstract partial class Human : Animal
{
	protected override void HandleAnimation()
	{
		var animHelper = new CitizenAnimationHelper( this );

		animHelper.WithWishVelocity( Velocity );
		animHelper.WithVelocity( Velocity );
		animHelper.WithLookAt( Position + Rotation.Forward * 100f, 1f, 1f, 0.5f );
		animHelper.AimAngle = Rotation;
		animHelper.DuckLevel = 0f;
		animHelper.VoiceLevel = 0f;
		animHelper.IsGrounded = GroundEntity.IsValid();
		animHelper.IsSitting = false;
		animHelper.IsNoclipping = false;
		animHelper.IsClimbing = false;
		animHelper.IsSwimming = false;
		animHelper.IsWeaponLowered = false;
		animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
		animHelper.AimBodyWeight = 0.5f;
	}
}
