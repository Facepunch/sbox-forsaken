using Sandbox;

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
		if ( IsServer )
		{
			if ( Provider is StorageCrate crate )
			{
				var item = InventorySystem.CreateItem<StorageCrateItem>();
				player.TryGiveItem( item );
				player.PlaySound( "inventory.move" );
				crate.Delete();
			}
			else if ( Provider is ItemEntity entity )
			{
				var item = entity.Take();
				 
				if ( item.IsValid() )
				{
					player.TryGiveItem( item );
					player.PlaySound( "inventory.move" );
				}
			}
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
