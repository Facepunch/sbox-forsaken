using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class ItemEntity : ModelEntity, IContextActionProvider
{
	[Net] public NetInventoryItem Item { get; private set; }

	public TimeUntil TimeUntilCanPickup { get; set; }

	public float MaxInteractRange => 150f;
	public Color GlowColor => Item.Value?.Color ?? Color.White;
	public float GlowWidth => 0.4f;

	public string GetContextName()
	{
		return Item.Value?.Name ?? "Unknown Item";
	}

	public List<ContextAction> GetSecondaryActions()
	{
		return null;
	}

	public ContextAction GetPrimaryAction()
	{
		var pickup = new PickupAction( this );
		return pickup;
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

	public virtual void Reset()
	{
		Delete();
	}

	public override void Spawn()
	{
		TimeUntilCanPickup = 1f;
		base.Spawn();
	}
}

