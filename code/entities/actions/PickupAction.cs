namespace Facepunch.Forsaken;

public class PickupAction : ContextAction
{
	public override string Name => "Pickup";
	public override string Icon => "textures/ui/actions/pickup.png";

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

	public override bool IsAvailable( ForsakenPlayer player )
	{
		if ( Provider is StorageCrate crate )
		{
			return crate.IsEmpty;
		}

		return true;
	}
}
