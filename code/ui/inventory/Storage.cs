using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

[UseTemplate]
public partial class Storage : Panel, IDialog
{
	public static Storage Current { get; private set; }

	public InventoryContainer StorageContainer { get; private set; }
	public InventoryContainer BackpackContainer { get; private set; }
	public InventoryContainer HotbarContainer { get; private set; }
	public List<InventorySlot> BackpackSlots { get; private set; }
	public List<InventorySlot> StorageSlots { get; private set; }
	public List<InventorySlot> HotbarSlots { get; private set; }
	public Panel BackpackSlotRoot { get; set; }
	public Panel StorageSlotRoot { get; set; }
	public Panel HotbarSlotRoot { get; set; }
	public bool IsOpen { get; set; }
	public Entity Entity { get; private set; }
	public string Name { get; private set; }

	public Storage()
	{
		BackpackSlots = new();
		StorageSlots = new();
		HotbarSlots = new();
		Current = this;
	}

	public void Open()
	{
		if ( IsOpen ) return;
		IDialog.Activate( this );
		IsOpen = true;
	}

	public void Close()
	{
		if ( !IsOpen ) return;
		IDialog.Deactivate( this );
		IsOpen = false;
	}

	public void SetName( string name )
	{
		Name = name;
	}

	public void SetEntity( Entity entity )
	{
		Entity = entity;
	}

	public void SetContainer( InventoryContainer container )
	{
		if ( Local.Pawn is not Player player )
			return;

		BackpackSlots ??= new();
		StorageSlots ??= new();
		HotbarSlots ??= new();

		BackpackSlotRoot.DeleteChildren( true );
		StorageSlotRoot.DeleteChildren( true );
		HotbarSlotRoot.DeleteChildren( true );

		BackpackSlots.Clear();
		StorageSlots.Clear();
		HotbarSlots.Clear();

		StorageContainer = container;
		StorageContainer.SetTransferTargetHandler( GetStorageTransferTarget );

		BackpackContainer = player.BackpackInventory.Instance;
		HotbarContainer = player.HotbarInventory.Instance;

		for ( ushort i = 0; i < container.SlotLimit; i++ )
		{
			var slot = StorageSlotRoot.AddChild<InventorySlot>();
			slot.Container = container;
			slot.Slot = i;
			StorageSlots.Add( slot );
		}

		for ( ushort i = 0; i < BackpackContainer.SlotLimit; i++ )
		{
			var slot = BackpackSlotRoot.AddChild<InventorySlot>();
			slot.Container = BackpackContainer;
			slot.Slot = i;
			BackpackSlots.Add( slot );
		}

		for ( ushort i = 0; i < HotbarContainer.SlotLimit; i++ )
		{
			var slot = HotbarSlotRoot.AddChild<InventorySlot>();
			slot.Container = HotbarContainer;
			slot.Slot = i;
			HotbarSlots.Add( slot );
		}
	}

	public override void Tick()
	{
		if ( Local.Pawn is not Player player )
			return;

		for ( ushort i = 0; i < BackpackSlots.Count; i++ )
		{
			var item = BackpackContainer.GetFromSlot( i );

			BackpackSlots[i].SetItem( item );
			BackpackSlots[i].IsSelected = false;
		}

		for ( ushort i = 0; i < HotbarSlots.Count; i++ )
		{
			var item = HotbarContainer.GetFromSlot( i );

			HotbarSlots[i].SetItem( item );
			HotbarSlots[i].IsSelected = false;
		}

		for ( ushort i = 0; i < StorageSlots.Count; i++)
		{
			var item = StorageContainer.GetFromSlot( i );

			StorageSlots[i].SetItem( item );
			StorageSlots[i].IsSelected = false;
		}

		base.Tick();
	}

	protected override void PostTemplateApplied()
	{
		base.PostTemplateApplied();

		if ( Local.Pawn is Player player && StorageContainer.IsValid() )
		{
			SetContainer( StorageContainer );
		}

		BindClass( "hidden", () => !IsOpen );
	}

	private InventoryContainer GetStorageTransferTarget( InventoryItem item )
	{
		return BackpackContainer;
	}
}
