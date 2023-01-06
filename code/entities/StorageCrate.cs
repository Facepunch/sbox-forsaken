using Sandbox;
using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

public partial class StorageCrate : Deployable, IContextActionProvider, IPersistence
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.Green;
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

	public bool ShouldSaveState()
	{
		return true;
	}

	public void BeforeStateLoaded()
	{

	}

	public void AfterStateLoaded()
	{

	}

	public void SerializeState( BinaryWriter writer )
	{
		writer.Write( Transform );
		writer.Write( Inventory );
	}

	public void DeserializeState( BinaryReader reader )
	{
		Transform = reader.ReadTransform();

		var container = reader.ReadInventoryContainer();
		InternalInventory = new( container );
		IsEmpty = container.IsEmpty;
	}

	public string GetContextName()
	{
		return "Storage Crate";
	}

	public void Open( ForsakenPlayer player )
	{
		UI.Storage.Open( player, GetContextName(), this, Inventory );
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield return OpenAction;
		yield return PickupAction;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return OpenAction;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == OpenAction )
		{
			if ( Game.IsServer )
			{
				Open( player );
			}
		}
		else if ( action == PickupAction )
		{
			if ( Game.IsServer )
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

		Tags.Add( "hover", "solid" );

		base.Spawn();
	}

	private void OnSlotChanged( ushort slot )
	{
		if ( Game.IsServer )
		{
			IsEmpty = Inventory.IsEmpty;
		}
	}
}
