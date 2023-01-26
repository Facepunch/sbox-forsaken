using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class Hud
{
	private static string LastZoneName { get; set; }
	private static TimeSince LastZoneShown { get; set; }

	[ClientRpc]
	[ConCmd.Client]
    public static void ShowZoneName( string name )
    {
		if ( LastZoneName == name && LastZoneShown < 10f )
			return;

		Current?.ShowZoneName( name, 4f );
	}
}
