using Sandbox;
using System.Collections.Generic;
using static Sandbox.Event;

namespace Facepunch.Forsaken;

public partial class ItemEntity : ModelEntity, IContextActionProvider
{
	[Net] public NetInventoryItem Item { get; private set; }

	public TimeUntil TimeUntilCanPickup { get; set; }

	public float InteractionRange => 150f;
	public Color GlowColor => Item.Value?.Color ?? Color.White;
	public float GlowWidth => 0.4f;

	private ContextAction PickupAction { get; set; }

	public ItemEntity()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
	}

	public string GetContextName()
	{
		if ( !Item.IsValid() ) return "Unknown Item";

		var item = Item.Value;

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

		Item = new NetInventoryItem( item );
		item.SetWorldEntity( this );

		Tags.Add( "item" );
	}

	public InventoryItem Take()
	{
		if ( IsValid )
		{
			var item = Item.Value;

			item.ClearWorldEntity();
			Item = null;
			Delete();

			return item;
		}

		return null;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == PickupAction )
		{
			if ( IsServer )
			{
				var item = Take();

				if ( item.IsValid() )
				{
					player.TryGiveItem( item );
					player.PlaySound( "inventory.move" );
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

