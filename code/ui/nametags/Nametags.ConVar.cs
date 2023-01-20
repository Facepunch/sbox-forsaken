using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class Nametags
{
    [ConVar.Client( "fsk.nametag.self" )]
    public static bool ShowOwnNametag { get; set; }
}
