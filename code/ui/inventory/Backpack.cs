using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

[UseTemplate]
public partial class Backpack : Panel, IDialog
{
	public static Backpack Current { get; private set; }

	public InventoryContainer BackpackContainer { get; private set; }
	public InventoryContainer EquipmentContainer { get; private set; }
	public InventoryContainer HotbarContainer { get; private set; }
	public List<InventorySlot> BackpackSlots { get; private set; }
	public List<InventorySlot> EquipmentSlots { get; private set; }
	public List<InventorySlot> HotbarSlots { get; private set; }
	public Panel BackpackSlotRoot { get; set; }
	public Panel EquipmentSlotRoot { get; set; }
	public Panel HotbarSlotRoot { get; set; }
	public bool IsOpen { get; set; }

	public Backpack()
	{
		BackpackSlots = new();
		EquipmentSlots = new();
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

	public void SetHotbar( InventoryContainer hotbar )
	{
		HotbarSlots ??= new();
		HotbarContainer = hotbar;
		HotbarSlotRoot.DeleteChildren( true );
		HotbarSlots.Clear();

		for ( ushort i = 0; i < hotbar.SlotLimit; i++ )
		{
			var slot = HotbarSlotRoot.AddChild<InventorySlot>();
			slot.OnSelected = OnHotbarSlotSelected;
			slot.Container = hotbar;
			slot.Slot = i;
			HotbarSlots.Add( slot );
		}
	}

	public void SetBackpack( InventoryContainer backpack )
	{
		BackpackSlots ??= new();
		BackpackContainer = backpack;
		BackpackSlotRoot.DeleteChildren( true );
		BackpackSlots.Clear();

		for ( ushort i = 0; i < backpack.SlotLimit; i++ )
		{
			var slot = BackpackSlotRoot.AddChild<InventorySlot>();
			slot.OnSelected = OnBackpackSlotSelected;
			slot.Container = backpack;
			slot.Slot = i;
			BackpackSlots.Add( slot );
		}
	}

	public void SetEquipment( InventoryContainer equipment )
	{
		EquipmentSlots ??= new();
		EquipmentContainer = equipment;
		EquipmentSlotRoot.DeleteChildren( true );
		EquipmentSlots.Clear();

		for ( ushort i = 0; i < equipment.SlotLimit; i++ )
		{
			var slot = EquipmentSlotRoot.AddChild<InventorySlot>();

			slot.OnSelected = OnEquipmentSlotSelected;
			slot.Container = equipment;
			slot.Slot = i;

			if ( i == 0 )
			{
				slot.SetDefaultIcon( "textures/ui/armor_slot_head.png" );
				slot.SetArmorSlot( ArmorSlot.Head );
			}
			else if ( i == 1 )
			{
				slot.SetDefaultIcon( "textures/ui/armor_slot_chest.png" );
				slot.SetArmorSlot( ArmorSlot.Chest );
			}
			else if ( i == 2 )
			{
				slot.SetDefaultIcon( "textures/ui/armor_slot_legs.png" );
				slot.SetArmorSlot( ArmorSlot.Legs );
			}

			EquipmentSlots.Add( slot );
		}
	}

	public override void Tick()
	{
		if ( Local.Pawn is not Player ) return;

		for ( ushort i = 0; i < BackpackSlots.Count; i++)
		{
			var item = BackpackContainer.GetFromSlot( i );
			BackpackSlots[i].SetItem( item );

			if ( !item.IsValid() )
				BackpackSlots[i].IsSelected = false;
		}

		for ( ushort i = 0; i < EquipmentSlots.Count; i++ )
		{
			var item = EquipmentContainer.GetFromSlot( i );
			EquipmentSlots[i].SetItem( item );

			if ( !item.IsValid() )
				EquipmentSlots[i].IsSelected = false;
		}

		for ( ushort i = 0; i < HotbarSlots.Count; i++ )
		{
			var item = HotbarContainer.GetFromSlot( i );
			HotbarSlots[i].SetItem( item );

			if ( !item.IsValid() )
				HotbarSlots[i].IsSelected = false;
		}

		base.Tick();
	}

	protected virtual void OnBackpackSlotSelected( InventorySlot slot )
	{
		ClearSelectedSlots();
		slot.IsSelected = true;
		Sound.FromScreen( "inventory.select" );
	}

	protected virtual void OnEquipmentSlotSelected( InventorySlot slot )
	{
		ClearSelectedSlots();
		slot.IsSelected = true;
		Sound.FromScreen( "inventory.select" );
	}

	protected virtual void OnHotbarSlotSelected( InventorySlot slot )
	{
		ClearSelectedSlots();
		slot.IsSelected = true;
		Sound.FromScreen( "inventory.select" );
	}

	protected override void PostTemplateApplied()
	{
		base.PostTemplateApplied();

		if ( Local.Pawn is Player player )
		{
			if ( player.BackpackInventory.IsValid() )
				SetBackpack( player.BackpackInventory.Instance );

			if ( player.EquipmentInventory.IsValid() )
				SetEquipment( player.EquipmentInventory.Instance );

			if ( player.HotbarInventory.IsValid() )
				SetHotbar( player.HotbarInventory.Instance );
		}

		BindClass( "hidden", () => !IsOpen );
	}

	private void ClearSelectedSlots()
	{
		EquipmentSlots.ForEach( s => s.IsSelected = false );
		BackpackSlots.ForEach( s => s.IsSelected = false );
		HotbarSlots.ForEach( s => s.IsSelected = false );
	}
}
