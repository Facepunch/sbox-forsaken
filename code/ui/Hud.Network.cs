using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class Hud
{
	[ClientRpc]
	[ConCmd.Client]
    public static void ShowZoneName( string name )
    {
		Current.ShowZoneName( name, 4f );
	}
}
