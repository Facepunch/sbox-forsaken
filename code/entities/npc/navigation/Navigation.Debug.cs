using Sandbox;

namespace Facepunch.Forsaken;

public partial class Navigation
{
	[Event.Debug.Overlay( "navigation", "Navigation", "square" )]
	public static void NavigationDebugOverlay()
	{
		if ( Game.IsClient ) return;
		if ( !IsReady ) return;

		for ( int x = 0; x < Grid.Length; x++ )
		{
			var color = Grid[x].Walkable ? Color.Green : Color.Red;
			DebugOverlay.Sphere( ToWorld( x ).WithZ( Grid[x].ZOffset ), 1f, color, 0f );
		}
	}
}
