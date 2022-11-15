using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class StorageCrate : Deployable, IContextActionProvider
{
	public float MaxInteractRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	[Net] private NetInventoryContainer InternalInventory { get; set; }
	public InventoryContainer Inventory => InternalInventory.Value;

	public string GetContextName()
	{
		return "Storage Crate";
	}

	public void Open( ForsakenPlayer player )
	{
		UI.Storage.Open( player, GetContextName(), this, Inventory );
	}

	public List<ContextAction> GetSecondaryActions()
	{
		return new List<ContextAction>()
		{
			new OpenAction( this ),
			new PickupAction( this )
		};
	}

	public ContextAction GetPrimaryAction()
	{
		var open = new OpenAction( this );
		return open;
	}

	public override void Spawn()
	{
		SetModel( "models/citizen_props/crate01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		var inventory = new InventoryContainer( this );
		inventory.SetSlotLimit( 16 );
		InventorySystem.Register( inventory );

		InternalInventory = new NetInventoryContainer( inventory );

		base.Spawn();
	}
}
