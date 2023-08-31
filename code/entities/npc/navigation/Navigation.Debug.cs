using Sandbox;

namespace Facepunch.Forsaken;

public partial class Navigation
{
	[Event.Debug.Overlay( "pathing", "Pathing", "square" )]
	public static void NavigationDebugOverlay()
	{
		if ( Game.IsClient ) return;
		if ( !IsReady ) return;
		
		var trace = Trace.Ray( Camera.Main.Position, Camera.Main.Rotation.Forward * 10000f ).Run();
		var distance = 300f * 300f;

		for ( int x = 0; x < Grid.Length; x++ )
		{
			var position = ToWorld( x ).WithZ( Grid[x].ZOffset );

			if ( position.DistanceSquared( trace.EndPosition ) <= distance )
			{
				var color = Grid[x].Walkable ? Color.Green : Color.Red;
				DebugOverlay.Sphere( position, 1f, color, 0f );
			}
		}
	}
}
