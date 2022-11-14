namespace Facepunch.Forsaken;

public class PickupAction : ContextAction
{
	public override string Name => "Pickup";
	public override string Icon => "textures/ui/armor_slot_head.png";

	public PickupAction()
	{

	}

	public PickupAction( IContextActionProvider owner )
	{
		Provider = owner;
	}

	public override void Select( ForsakenPlayer player )
	{
		if ( IsServer && Provider is StorageCrate crate )
		{
			var item = InventorySystem.CreateItem<CrateItem>();
			player.TryGiveItem( item );
			player.PlaySound( "inventory.move" );
			crate.Delete();
		}
	}
}
