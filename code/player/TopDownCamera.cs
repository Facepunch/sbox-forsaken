using Sandbox;

namespace Facepunch.Forsaken;

public partial class TopDownCamera
{
	public float Height { get; set; } = 450f;
	public float MoveSpeed { get; set; } = 20f;

	public void Update()
	{
		var pawn = ForsakenPlayer.Me;

		if ( pawn.IsValid() )
		{
			var target = pawn.Position.WithZ( pawn.Position.z + Height );

			Sound.Listener = new Transform( pawn.EyePosition, Camera.Rotation );

			Camera.Position = Camera.Position.LerpTo( target, Time.Delta * MoveSpeed );
			Camera.Rotation = Rotation.LookAt( Vector3.Down );
			Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 50f );
			Camera.FirstPersonViewer = null;
		}
	}
}
