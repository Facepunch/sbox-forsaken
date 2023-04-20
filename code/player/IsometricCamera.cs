using Sandbox;

namespace Facepunch.Forsaken;

public partial class IsometricCamera
{
	public float ZoomLevel { get; set; } = 0f;
	public float MinimumZoom => 200f;
	public float MaximumZoom => 900f;

	private Vector3 LookAt { get; set; }
	private Vector3 Offset { get; set; }

	public void Update()
	{
		var pawn = ForsakenPlayer.Me;

		if ( pawn.IsValid() )
		{
			if ( Input.Down( "walk" ) )
			{
				ZoomLevel += Input.MouseWheel * Time.Delta * 8f;
			}

			ZoomLevel = ZoomLevel.Clamp( 0f, 1f );

			Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 30f );

			LookAt = pawn.Position + Vector3.Up * 50f;

			var position = LookAt + Vector3.Backward * (MinimumZoom - (MinimumZoom * ZoomLevel * 0.6f));
			position += (Vector3.Right - 1f) * (MinimumZoom - (MinimumZoom * ZoomLevel * 0.6f));
			position += Vector3.Up * (MaximumZoom - (MaximumZoom * ZoomLevel * 0.6f));

			var trace = Trace.Ray( LookAt, position )
				.WithAnyTags( "world" )
				.WithoutTags( "solid" )
				.Radius( 2f )
				.Run();

			Offset = LookAt - position;

			Camera.Rotation = Rotation.Slerp( Camera.Rotation, Rotation.LookAt( Offset, Vector3.Up ), Time.Delta * 8f );
			Camera.Position = trace.EndPosition;

			Sound.Listener = new Transform( LookAt, Rotation.FromYaw( 45f ) );

			Camera.FirstPersonViewer = null;

			ScreenShake.Apply();
		}
	}
}
