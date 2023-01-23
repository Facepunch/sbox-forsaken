using Sandbox;

namespace Facepunch.Forsaken;

public partial class HotbarContainer : InventoryContainer
{
	public HotbarContainer() : base()
	{
		SetSlotLimit( 8 );
	}

	public override InventoryContainer GetTransferTarget( InventoryItem item )
	{
		return UI.Storage.Current.IsOpen ? UI.Storage.Current.Container : ForsakenPlayer.Me.Backpack;
	}
}
