using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class PlayerAnimator : PawnAnimator
{
	private float InterpolateDuck { get; set; }

	public override void Simulate()
	{
		var player = Pawn as Player;
		var rotation = Rotation;

		// If we're a bot, spin us around 180 degrees.
		if ( player.Client.IsBot )
			rotation = player.ViewAngles.WithYaw( player.ViewAngles.yaw + 180f ).ToRotation();
		else
			rotation = player.ViewAngles.ToRotation();

		if ( Input.Down( InputButton.Run ) )
		{
			rotation = Rotation.LookAt( player.InputDirection, Vector3.Up );
		}

		DoRotation( rotation );
		DoWalk();

		var isSitting = HasTag( "sitting" );

		SetAnimParameter( "b_grounded", GroundEntity != null || isSitting );
		SetAnimParameter( "b_noclip", false );
		SetAnimParameter( "b_sit", isSitting );
		SetAnimParameter( "b_swim", Pawn.WaterLevel > 0.5f && !isSitting );

		if ( Host.IsClient && Client.IsValid() )
		{
			SetAnimParameter( "voice", Client.TimeSinceLastVoice < 0.5f ? Client.VoiceLevel : 0f );
		}

		var aimPosition = Pawn.EyePosition + rotation.Forward * 200f;
		var lookPosition = aimPosition;

		SetLookAt( "aim_eyes", lookPosition );
		SetLookAt( "aim_head", lookPosition );
		SetLookAt( "aim_body", aimPosition );

		if ( HasTag( "ducked" ) )
			InterpolateDuck = InterpolateDuck.LerpTo( 1f, Time.Delta * 10f );
		else
			InterpolateDuck = InterpolateDuck.LerpTo( 0f, Time.Delta * 5f );

		SetAnimParameter( "duck", InterpolateDuck );

		if ( player != null && player.ActiveChild is BaseCarriable carry )
		{
			carry.SimulateAnimator( this );
		}
		else
		{
			SetAnimParameter( "holdtype", 0 );
			SetAnimParameter( "aim_body_weight", 0.5f );
		}

	}

	public virtual void DoRotation( Rotation idealRotation )
	{
		Rotation = Rotation.Lerp( Rotation, idealRotation, Time.Delta * 10f );
	}

	private void DoWalk()
	{
		{
			var direction = Velocity;
			var forward = Rotation.Forward.Dot( direction );
			var sideward = Rotation.Right.Dot( direction );
			var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

			SetAnimParameter( "move_direction", angle );
			SetAnimParameter( "move_speed", Velocity.Length );
			SetAnimParameter( "move_groundspeed", Velocity.WithZ( 0 ).Length );
			SetAnimParameter( "move_y", sideward );
			SetAnimParameter( "move_x", forward );
			SetAnimParameter( "move_z", Velocity.z );
		}

		{
			var direction = WishVelocity;
			var forward = Rotation.Forward.Dot( direction );
			var sideward = Rotation.Right.Dot( direction );
			var angle = MathF.Atan2( sideward, forward ).RadianToDegree().NormalizeDegrees();

			SetAnimParameter( "wish_direction", angle );
			SetAnimParameter( "wish_speed", WishVelocity.Length );
			SetAnimParameter( "wish_groundspeed", WishVelocity.WithZ( 0 ).Length );
			SetAnimParameter( "wish_y", sideward );
			SetAnimParameter( "wish_x", forward );
			SetAnimParameter( "wish_z", WishVelocity.z );
		}
	}
}
