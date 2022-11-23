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

	private ContextAction PickupAction { get; set; }
	private ContextAction OpenAction { get; set; }

	[Net] public bool IsEmpty { get; private set; }

	public StorageCrate()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
		PickupAction.SetCondition( p => IsEmpty );

		OpenAction = new( "open", "Open", "textures/ui/actions/open.png" );
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

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == OpenAction )
		{
			if ( IsServer )
			{
				Open( player );
			}
		}
		else if ( action == PickupAction )
		{
			if ( IsServer )
			{
				var item = InventorySystem.CreateItem<StorageCrateItem>();
				player.TryGiveItem( item );
				player.PlaySound( "inventory.move" );
				Delete();
			}
		}
	}

	public override void Spawn()
	{
		SetModel( "models/citizen_props/crate01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		var inventory = new InventoryContainer();
		inventory.SetEntity( this );
		inventory.SetSlotLimit( 16 );
		inventory.SlotChanged += OnSlotChanged;
		InventorySystem.Register( inventory );

		InternalInventory = new NetInventoryContainer( inventory );
		IsEmpty = inventory.IsEmpty;

		base.Spawn();
	}

	private void OnSlotChanged( ushort slot )
	{
		if ( IsServer )
		{
			IsEmpty = Inventory.IsEmpty;
		}
	}
}
