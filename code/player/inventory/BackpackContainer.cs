using Sandbox;

namespace Facepunch.Forsaken;

public partial class BackpackContainer : InventoryContainer
{
	public BackpackContainer() : base()
	{
		SetSlotLimit( 24 );
	}

	public override InventoryContainer GetTransferTarget( InventoryItem item )
	{
		var cooking = UI.Cooking.Current;
		var storage = UI.Storage.Current;

		if ( cooking.IsOpen )
		{
			var processor = cooking.Cooker.Processor;

			if ( processor.Fuel.DoesPassFilter( item ) )
				return processor.Fuel;

			if ( processor.Input.DoesPassFilter( item ) )
				return processor.Input;
		}

		if ( storage.IsOpen )
		{
			return storage.Container;
		}

		return storage.IsOpen ? storage.Container : ForsakenPlayer.Me.Hotbar;
	}
}
