using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken.UI;

public class MinimapNoEntitiesHook : RenderHook
{
	private HashSet<SceneObject> HiddenSceneObjects { get; set; }

	public override void OnFrame( SceneCamera target )
	{
		HiddenSceneObjects ??= new();
		HiddenSceneObjects.Clear();

		foreach ( var so in Game.SceneWorld.SceneObjects )
		{
			if ( so is not SceneModel )
				continue;

			if ( !so.RenderingEnabled )
			{
				HiddenSceneObjects.Add( so );
			}

			so.RenderingEnabled = false;
		}

		base.OnFrame( target );
	}

	public override void OnStage( SceneCamera target, Stage renderStage )
	{
		if ( renderStage == Stage.AfterPostProcess )
		{
			foreach ( var so in Game.SceneWorld.SceneObjects )
			{
				if ( !HiddenSceneObjects.Contains( so ) )
					so.RenderingEnabled = true;
			}
		}

		base.OnStage( target, renderStage );
	}
}

public partial class Minimap
{
    public static Texture RenderTexture { get; private set; }
	public static SceneCamera Camera { get; private set; }

	public static void Render( Vector3 position )
	{
		var cameraPosition = position;
		cameraPosition.z += 2000f;

		if ( Camera == null )
		{
			Camera = new SceneCamera( "Minimap" );
			Camera.World = Game.SceneWorld;
		}

		Camera.FindOrCreateHook<MinimapNoEntitiesHook>();

		Camera.Position = cameraPosition;
		Camera.Rotation = Rotation.From( new Angles( 90f, 45f, 0f ) );
		Camera.FieldOfView = 60f;
		Camera.BackgroundColor = Color.Black;
		Camera.FirstPersonViewer = null;
		Camera.ZFar = 5000f;
		Camera.ZNear = 0.1f;

		Graphics.RenderToTexture( Camera, GetOrCreateTexture() );
	}

	public static Texture GetOrCreateTexture()
	{
		if ( RenderTexture is not null ) return RenderTexture;
		RenderTexture = Texture.CreateRenderTarget( "Minimap", ImageFormat.RGBA8888, new Vector2( 512, 512f ) );
		return RenderTexture;
	}
}
