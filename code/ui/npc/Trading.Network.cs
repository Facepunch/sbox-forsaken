using Sandbox;
using Conna.Inventory;

namespace Facepunch.Forsaken.UI;

public partial class Trading
{
    public static void Open( ForsakenPlayer player, Trader trader )
    {
        OpenForClient( To.Single( player ), trader, trader.Inventory.Serialize() );

		var viewer = player.Client.Components.Get<InventoryViewer>();
		viewer?.AddContainer( trader.Inventory );
    }

    [ClientRpc]
    public static void OpenForClient( Trader trader, byte[] inventory )
    {
        if ( Game.LocalPawn is not ForsakenPlayer ) return;

        var trading = Current;
		var container = InventoryContainer.Deserialize( inventory );

		trading.SetInventory( container );
		trading.SetTrader( trader );
		trading.Open();

        Sound.FromScreen( "inventory.open" );
    }
}
