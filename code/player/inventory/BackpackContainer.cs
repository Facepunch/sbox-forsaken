using Sandbox;
using Conna.Inventory;

namespace Facepunch.Forsaken;

public partial class BackpackContainer : InventoryContainer
{
	public BackpackContainer() : base()
	{
		SetSlotLimit( 24 );
	}

	public override InventoryContainer GetTransferTarget( InventoryItem item )
	{
		var recycling = UI.Recycling.Current;
		var cooking = UI.Cooking.Current;
		var storage = UI.Storage.Current;

		if ( recycling.IsOpen )
		{
			var processor = recycling.Recycler.Processor;

			if ( processor.Input.DoesPassFilter( item ) )
				return processor.Input;

			return null;
		}

		if ( cooking.IsOpen )
		{
			var processor = cooking.Cooker.Processor;

			if ( processor.Fuel.DoesPassFilter( item ) )
				return processor.Fuel;

			if ( processor.Input.DoesPassFilter( item ) )
				return processor.Input;

			return null;
		}

		if ( storage.IsOpen )
		{
			return storage.Container;
		}

		var equipment = ForsakenPlayer.Me.Equipment;

		if ( item is ArmorItem && equipment.CouldTakeAny( item ) )
		{
			return equipment;
		}

		return ForsakenPlayer.Me.Hotbar;
	}
}
