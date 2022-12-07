using Facepunch.Forsaken.UI;
using Sandbox;
using System;

namespace Facepunch.Forsaken;

[SceneCamera.AutomaticRenderHook]
public class PlayerPeek : RenderHook
{
	public static SceneCamera PeekCamera = new();
	public static Material PlayerViewMaterial = Material.FromShader("PlayerViewer.vfx");
	public static Texture PlayerView = null;
	public static float Poop = 100.0f;
	public static bool IsRenderingToTexture = false;
	public static Vector2 LastDistance = 0.0f;
	public static float CurrentRadius = 0.05f;
	public static float MaxRadius = 0.15f;
	public static float MinRadius = 0.05f;


	[Event.Frame]
	private static void render()
	{
		PlayerView = Texture.CreateRenderTarget("Player Viewer", ImageFormat.RGBA8888, Screen.Size, PlayerView);
		PeekCamera.World = Map.Scene;
		PeekCamera.Name = "PlayerViewer";

		float znear = PeekCamera.ZNear;
		float zfar = PeekCamera.ZFar;
		float fov = PeekCamera.FieldOfView;

		PeekCamera.ZNear = 360.0f;
		PeekCamera.ZFar = 360.0f + Poop + 500.0f;
		PeekCamera.FieldOfView = (MathF.Atan(MathF.Tan(fov.DegreeToRadian() * 0.5f) * (Screen.Aspect * 0.75f)).RadianToDegree() * 2.0f);
		PeekCamera.EnablePostProcessing = true;
		PeekCamera.AmbientLightColor = Color.Black;

		/*Plane clipPlane = new();
		clipPlane.Normal = Vector3.Down;
		clipPlane.Distance = Local.Pawn.Position.z + 360.0f;

		PeekCamera.Attributes.SetCombo("D_ENABLE_USER_CLIP_PLANE", true);
		PeekCamera.Attributes.Set("EnableClipPlane", true);
		PeekCamera.Attributes.Set("ClipPlane0", new Vector4(clipPlane.Normal, clipPlane.Distance));*/

		IsRenderingToTexture = true;
		Graphics.RenderToTexture(PeekCamera, PlayerView);
		IsRenderingToTexture = false;


		PeekCamera.ZNear = znear;
		PeekCamera.ZFar = zfar;
		PeekCamera.FieldOfView = fov;
	}


	public override void OnStage(SceneCamera target, Stage renderStage)
	{
		if (IsRenderingToTexture) return;

		if (renderStage == Stage.BeforePostProcess)
		{
			PeekCamera = target;
			Graphics.RenderTarget = null;

			Vector2 cursor = (Local.Pawn as ForsakenPlayer).Cursor;
			float dist = cursor.Distance(LastDistance);
			if (dist > 0.01f) CurrentRadius = CurrentRadius.LerpTo(MinRadius, 0.2f, true);
			else CurrentRadius = CurrentRadius.LerpTo(MaxRadius, 0.005f, true);

			LastDistance = cursor;

			RenderAttributes attributes = new();
			attributes.Set("PlayerTexture", PlayerView);
			Graphics.GrabFrameTexture("ColorBuffer", attributes);
			attributes.Set("CursorUvs", cursor);
			attributes.Set("CursorScale", CurrentRadius);
			Graphics.Blit(PlayerViewMaterial, attributes);
		}
	}
}

