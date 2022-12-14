using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class Storage
{
    public static void Open( ForsakenPlayer player, string name, Entity entity, InventoryContainer container )
    {
        OpenForClient( To.Single(player), name, entity, container.Serialize() );

		var viewer = player.Client.Components.Get<InventoryViewer>();
		viewer?.AddContainer( container );
    }

    [ClientRpc]
    public static void OpenForClient( string name, Entity entity, byte[] data )
    {
        if ( Game.LocalPawn is not ForsakenPlayer ) return;

        var container = InventoryContainer.Deserialize( data );
        var storage = Current;

        storage.SetName( name );
        storage.SetEntity( entity );
        storage.SetContainer( container );
        storage.Open();

        Sound.FromScreen( "inventory.open" );
    }
}
