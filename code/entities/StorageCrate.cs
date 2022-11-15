using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class StorageCrate : Deployable, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	[Net] private NetInventoryContainer InternalInventory { get; set; }
	public InventoryContainer Inventory => InternalInventory.Value;

	private PickupAction PickupAction { get; set; }
	private OpenAction OpenAction { get; set; }

	public StorageCrate()
	{
		PickupAction = new( this );
		OpenAction = new( this );
	}

	public string GetContextName()
	{
		return "Storage Crate";
	}

	public void Open( ForsakenPlayer player )
	{
		UI.Storage.Open( player, GetContextName(), this, Inventory );
	}

	public IEnumerable<ContextAction> GetSecondaryActions()
	{
		yield return OpenAction;
		yield return PickupAction;
	}

	public ContextAction GetPrimaryAction()
	{
		return OpenAction;
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
