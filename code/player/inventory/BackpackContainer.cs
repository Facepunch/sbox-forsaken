using Sandbox;

namespace Facepunch.Forsaken;

public partial class BackpackContainer : InventoryContainer
{
	public BackpackContainer( Entity owner ) : base( owner )
	{
		SetSlotLimit( 24 );
	}

	public override InventoryContainer GetTransferTarget()
	{
		return UI.Storage.Current.IsOpen ? UI.Storage.Current.Container : ForsakenPlayer.Me.Hotbar;
	}
}
