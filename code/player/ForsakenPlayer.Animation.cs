using Sandbox;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer
{
	private Weapon LastWeaponEntity { get; set; }

	protected void SimulateAnimation()
	{
		Rotation rotation;

		// If we're a bot, spin us around 180 degrees.
		if ( Client.IsBot )
			rotation = ViewAngles.WithYaw( ViewAngles.yaw + 180f ).ToRotation();
		else
			rotation = ViewAngles.ToRotation();

		if ( Input.Down( InputButton.Run ) )
		{
			rotation = Rotation.LookAt( InputDirection, Vector3.Up );
		}

		Rotation = Rotation.Lerp( Rotation, rotation, Time.Delta * 10f );

		var animHelper = new CitizenAnimationHelper( this );

		animHelper.WithWishVelocity( Controller.WishVelocity );
		animHelper.WithVelocity( Velocity );
		animHelper.WithLookAt( EyePosition + EyeRotation.Forward * 100.0f, 1.0f, 1.0f, 0.5f );
		animHelper.AimAngle = rotation;
		animHelper.DuckLevel = MathX.Lerp( animHelper.DuckLevel, Controller.HasTag( "ducked" ) ? 1 : 0, Time.Delta * 10.0f );
		animHelper.VoiceLevel = (Game.IsClient && Client.IsValid()) ? Client.Voice.LastHeard < 0.5f ? Client.Voice.CurrentLevel : 0.0f : 0.0f;
		animHelper.IsGrounded = GroundEntity != null;
		animHelper.IsSitting = Controller.HasTag( "sitting" );
		animHelper.IsNoclipping = Controller.HasTag( "noclip" );
		animHelper.IsClimbing = Controller.HasTag( "climbing" );
		animHelper.IsSwimming = false;
		animHelper.IsWeaponLowered = false;

		if ( Controller.HasEvent( "jump" ) ) animHelper.TriggerJump();
		if ( ActiveChild != LastWeaponEntity ) animHelper.TriggerDeploy();

		if ( ActiveChild is Weapon weapon )
		{
			weapon.SimulateAnimator( animHelper );
		}
		else
		{
			animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
			animHelper.AimBodyWeight = 0.5f;
		}

		LastWeaponEntity = ActiveChild as Weapon;
	}
}
