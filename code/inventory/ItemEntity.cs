using Sandbox;
using System.Collections.Generic;
using static Sandbox.Event;

namespace Facepunch.Forsaken;

public partial class ItemEntity : ModelEntity, IContextActionProvider
{
	[Net] private NetInventoryItem InternalItem { get; set; }
	public InventoryItem Item => InternalItem.Value;

	public TimeUntil TimeUntilCanPickup { get; set; }

	public float InteractionRange => 150f;
	public Color GlowColor => Item?.Color ?? Color.White;
	public float GlowWidth => 0.4f;

	private ContextAction PickupAction { get; set; }

	public ItemEntity()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
	}

	public string GetContextName()
	{
		if ( !Item.IsValid() ) return "Unknown Item";

		var item = Item;

		if ( item.StackSize > 1 )
			return $"{item.Name} ({item.StackSize})";
		else
			return item.Name;
	}

	public IEnumerable<ContextAction> GetSecondaryActions()
	{
		yield break;
	}

	public ContextAction GetPrimaryAction()
	{
		return PickupAction;
	}

	public void SetItem( InventoryItem item )
	{
		var worldModel = !string.IsNullOrEmpty( item.WorldModel ) ? item.WorldModel : "models/sbox_props/burger_box/burger_box.vmdl";

		if ( !string.IsNullOrEmpty( worldModel ) )
		{
			SetModel( worldModel );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		}

		InternalItem = new NetInventoryItem( item );
		item.SetWorldEntity( this );

		Tags.Add( "item" );
	}

	public InventoryItem Take()
	{
		if ( IsValid && Item.IsValid() )
		{
			var item = Item;

			item.ClearWorldEntity();
			InternalItem = null;
			Delete();

			return item;
		}

		return null;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == PickupAction )
		{
			if ( IsServer && IsValid && Item.IsValid() )
			{
				var initialAmount = Item.StackSize;
				var remaining = player.TryGiveItem( Item );

				if ( remaining < initialAmount )
				{
					player.PlaySound( "inventory.move" );
				}

				if ( remaining == 0 )
				{
					Take();
				}
			}
		}
	}

	public virtual void Reset()
	{
		Delete();
	}

	public override void Spawn()
	{
		TimeUntilCanPickup = 1f;

		Tags.Add( "item" );

		base.Spawn();
	}
}

