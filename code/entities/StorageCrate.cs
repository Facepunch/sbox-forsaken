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
		Inventory.AddConnection( player.Client );
		OpenForClient( To.Single( player ), Inventory.Serialize() );
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

	[ClientRpc]
	private void OpenForClient( byte[] data )
	{
		if ( Local.Pawn is not Player ) return;

		var container = InventoryContainer.Deserialize( data );
		var storage = UI.Storage.Current;

		storage.SetName( GetContextName() );
		storage.SetEntity( this );
		storage.SetContainer( container );
		storage.Open();

		Sound.FromScreen( "inventory.open" );
	}
}
