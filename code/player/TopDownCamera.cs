using Sandbox;

namespace Facepunch.Forsaken;

public partial class TopDownCamera : CameraMode
{
	public float Height { get; set; } = 450f;
	public float MoveSpeed { get; set; } = 20f;

	public override void Update()
	{
		var pawn = ForsakenPlayer.Me;

		if ( pawn.IsValid() )
		{
			var target = pawn.Position.WithZ( pawn.Position.z + Height );

			Sound.Listener = new Transform( pawn.EyePosition, pawn.Rotation );

			Position = Position.LerpTo( target, Time.Delta * MoveSpeed );
			Rotation = Rotation.LookAt( Vector3.Down );
			FieldOfView = 70f;
			Viewer = null;
		}
	}
}
