using Sandbox;
using System;

namespace Facepunch.Forsaken;

/*
[SceneCamera.AutomaticRenderHook]
public class PlayerViewer : RenderHook
{
	private static SceneCamera PeekCamera { get; set; } = new();
	private static Material Material { get; set; } = Material.FromShader( "PlayerViewer.vfx" );
	private static Texture RenderTexture { get; set; } = null;
	private static float ViewDistance => 100f;
	private static bool IsRenderingToTexture { get; set; } = false;
	private static Vector2 LastCursorDistance { get; set; } = 0f;
	private static float CurrentCursorRadius { get; set; } = 0.05f;
	private static float MaxCursorRadius => 0.15f;
	private static float MinCursorRadius => 0.05f;
	private static bool IsEnabled => false;

	[Event.Client.Frame]
	private static void OnFrame()
	{
		if ( !ForsakenPlayer.Me.IsValid() ) return;
		if ( !IsEnabled ) return;

		RenderTexture = Texture.CreateRenderTarget( "Player Viewer", ImageFormat.RGBA8888, Screen.Size, RenderTexture );

		PeekCamera.World = Game.SceneWorld;
		PeekCamera.Name = "PlayerViewer";

		float znear = PeekCamera.ZNear;
		float zfar = PeekCamera.ZFar;
		float fov = PeekCamera.FieldOfView;

		PeekCamera.ZNear = 360f;
		PeekCamera.ZFar = 360f + ViewDistance + 500f;
		PeekCamera.FieldOfView = MathF.Atan( MathF.Tan( fov.DegreeToRadian() * 0.5f ) * (Screen.Aspect * 0.75f) ).RadianToDegree() * 2f;
		PeekCamera.EnablePostProcessing = true;
		PeekCamera.AmbientLightColor = Color.Black;

		IsRenderingToTexture = true;
		Graphics.RenderToTexture( PeekCamera, RenderTexture );
		IsRenderingToTexture = false;

		PeekCamera.ZNear = znear;
		PeekCamera.ZFar = zfar;
		PeekCamera.FieldOfView = fov;
	}

	public override void OnStage( SceneCamera target, Stage renderStage )
	{
		if ( !IsEnabled ) return;
		if ( IsRenderingToTexture ) return;
		if ( !ForsakenPlayer.Me.IsValid() ) return;

		if ( renderStage == Stage.BeforePostProcess )
		{
			PeekCamera = target;

			Graphics.RenderTarget = null;

			var cursor = ForsakenPlayer.Me.Cursor;
			var distance = cursor.Distance( LastCursorDistance );

			if ( distance > 0.01f )
				CurrentCursorRadius = CurrentCursorRadius.LerpTo( MinCursorRadius, 0.2f, true );
			else
				CurrentCursorRadius = CurrentCursorRadius.LerpTo( MaxCursorRadius, 0.005f, true );

			LastCursorDistance = cursor;

			CurrentCursorRadius = 0f;

			RenderAttributes attributes = new();

			attributes.Set( "PlayerTexture", RenderTexture );
			Graphics.GrabFrameTexture( "ColorBuffer", attributes );

			attributes.Set( "CursorUvs", cursor );
			attributes.Set( "CursorScale", CurrentCursorRadius );
			Graphics.Blit( Material, attributes );
		}
	}
}
*/
