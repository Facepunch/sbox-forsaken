using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class Cooking
{
    public static void Open( ForsakenPlayer player, string name, ICookerEntity entity )
    {
		var processor = entity.Processor;

        OpenForClient( To.Single(player), name, (Entity)entity, processor.Fuel.Serialize(), processor.Input.Serialize(), processor.Output.Serialize() );

		var viewer = player.Client.Components.Get<InventoryViewer>();
		viewer?.AddContainer( processor.Fuel );
		viewer?.AddContainer( processor.Input );
		viewer?.AddContainer( processor.Output );
    }

    [ClientRpc]
    public static void OpenForClient( string name, Entity entity, byte[] fuel, byte[] input, byte[] output )
    {
        if ( Game.LocalPawn is not ForsakenPlayer ) return;
		if ( entity is not ICookerEntity cooker ) return;

        var storage = Current;

		InventoryContainer.Deserialize( fuel );
		InventoryContainer.Deserialize( input );
		InventoryContainer.Deserialize( output );

		storage.SetName( name );
        storage.SetCooker( cooker );
        storage.Open();

        Sound.FromScreen( "inventory.open" );
    }
}
