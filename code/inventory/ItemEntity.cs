using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class ItemEntity : ModelEntity, IContextActionProvider
{
	[Net] public NetInventoryItem Item { get; private set; }

	public TimeUntil TimeUntilCanPickup { get; set; }

	public float InteractionRange => 150f;
	public Color GlowColor => Item.Value?.Color ?? Color.White;
	public float GlowWidth => 0.4f;

	private PickupAction PickupAction { get; set; }

	public ItemEntity()
	{
		PickupAction = new( this );
	}

	public string GetContextName()
	{
		return Item.Value?.Name ?? "Unknown Item";
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

