using Sandbox;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Player
{

	public List<T> FindItems<T>() where T : InventoryItem
	{
		var items = new List<T>();
		items.AddRange( HotbarInventory.Instance.FindItems<T>() );
		items.AddRange( BackpackInventory.Instance.FindItems<T>() );
		items.AddRange( EquipmentInventory.Instance.FindItems<T>() );
		return items;
	}

	public List<InventoryItem> FindItems( Type type )
	{
		var items = new List<InventoryItem>();
		items.AddRange( HotbarInventory.Instance.FindItems( type ) );
		items.AddRange( BackpackInventory.Instance.FindItems( type ) );
		items.AddRange( EquipmentInventory.Instance.FindItems( type ) );
		return items;
	}

	public ushort TakeAmmo( AmmoType type, ushort count )
	{
		var items = new List<AmmoItem>();

		items.AddRange( HotbarInventory.Instance.FindItems<AmmoItem>() );
		items.AddRange( BackpackInventory.Instance.FindItems<AmmoItem>() );

		var amountLeftToTake = count;
		ushort totalAmountTaken = 0;

		for ( int i = items.Count - 1; i >= 0; i-- )
		{
			var item = items[i];

			if ( item.AmmoType == type )
			{
				if ( item.StackSize >= amountLeftToTake )
				{
					totalAmountTaken += amountLeftToTake;
					item.StackSize -= amountLeftToTake;

					if ( item.StackSize > 0 )
						return totalAmountTaken;
				}
				else
				{
					amountLeftToTake -= item.StackSize;
					totalAmountTaken += item.StackSize;
					item.StackSize = 0;
				}

				item.Remove();
			}
		}

		return totalAmountTaken;
	}

	public int GetAmmoCount( AmmoType type )
	{
		var items = new List<AmmoItem>();

		items.AddRange( HotbarInventory.Instance.FindItems<AmmoItem>() );
		items.AddRange( BackpackInventory.Instance.FindItems<AmmoItem>() );

		var output = 0;

		foreach ( var item in items )
		{
			if ( item.AmmoType == type )
			{
				output += item.StackSize;
			}
		}

		return output;
	}

	public bool TryGiveArmor( ArmorItem item )
	{
		var slotToIndex = (int)item.ArmorSlot - 1;
		return EquipmentInventory.Instance.Give( item, (ushort)slotToIndex );
	}

	public ushort TryGiveItem( InventoryItem item )
	{
		var remaining = HotbarInventory.Instance.Stack( item );

		if ( remaining > 0 )
		{
			remaining = BackpackInventory.Instance.Stack( item );
		}

		return remaining;
	}

	public bool TryGiveWeapon<T>() where T : WeaponItem
	{
		var item = InventorySystem.CreateItem<T>();

		if ( HotbarInventory.Instance.Give( item ) )
			return true;

		return BackpackInventory.Instance.Give( item );
	}

	public void TryGiveAmmo( AmmoType type, ushort amount )
	{
		var resource = ResourceLibrary.GetAll<AmmoResource>()
			.Where( a => a.AmmoType == type )
			.FirstOrDefault();

		var item = InventorySystem.CreateItem<AmmoItem>();
		item.Resource = resource;
		item.StackSize = amount;

		var remaining = HotbarInventory.Instance.Stack( item );

		if ( remaining > 0 )
		{
			BackpackInventory.Instance.Stack( item );
		}
	}

	private bool CanGiveEquipmentItem( ushort slot, InventoryItem item )
	{
		if ( item is not ArmorItem armor )
			return false;

		if ( armor.ArmorSlot == ArmorSlot.Head )
			return slot == 0;

		if ( armor.ArmorSlot == ArmorSlot.Chest )
			return slot == 1;

		if ( armor.ArmorSlot == ArmorSlot.Legs )
			return slot == 2;

		return false;
	}

	private void AddClothingToArmorSlot( ArmorSlot slot, BaseClothing clothing )
	{
		if ( !Armor.TryGetValue( slot, out var models ) )
		{
			models = new List<BaseClothing>();
			Armor[slot] = models;
		}

		models.Add( clothing );
	}

	private void OnEquipmentItemGiven( ushort slot, InventoryItem instance )
	{
		if ( instance is ArmorItem armor )
		{
			if ( Armor.TryGetValue( armor.ArmorSlot, out var models ) )
			{
				foreach ( var model in models )
				{
					model.Delete();
				}

				Armor.Remove( armor.ArmorSlot );
			}

			if ( !string.IsNullOrEmpty( armor.PrimaryModel ) )
			{
				AddClothingToArmorSlot( armor.ArmorSlot, AttachClothing( armor.PrimaryModel ) );
			}

			if ( !string.IsNullOrEmpty( armor.SecondaryModel ) )
			{
				AddClothingToArmorSlot( armor.ArmorSlot, AttachClothing( armor.SecondaryModel ) );
			}
		}
	}

	private void OnEquipmentItemTaken( ushort slot, InventoryItem instance )
	{
		if ( instance is ArmorItem armor && !EquipmentInventory.Is( instance.Container ) )
		{
			if ( Armor.TryGetValue( armor.ArmorSlot, out var models ) )
			{
				foreach ( var model in models )
				{
					model.Delete();
				}

				Armor.Remove( armor.ArmorSlot );
			}
		}
	}

	private void OnBackpackItemGiven( ushort slot, InventoryItem instance )
	{

	}

	private void OnBackpackItemTaken( ushort slot, InventoryItem instance )
	{

	}

	private void OnHotbarItemGiven( ushort slot, InventoryItem instance )
	{
		if ( instance is WeaponItem weapon )
		{
			InitializeWeaponItem( weapon );
		}
	}

	private void OnHotbarItemTaken( ushort slot, InventoryItem instance )
	{
		if ( instance is WeaponItem weapon )
		{
			if ( weapon.Weapon.IsValid() && !HotbarInventory.Is( instance.Container ) )
			{
				weapon.Weapon.Delete();
				weapon.Weapon = null;
				weapon.IsDirty = true;
			}
		}
	}

	private InventoryContainer GetBackpackTransferTarget( InventoryItem item )
	{
		return Storage.Current.IsOpen ? Storage.Current.StorageContainer : HotbarInventory.Instance;
	}

	private InventoryContainer GetEquipmentTransferTarget( InventoryItem item )
	{
		return Storage.Current.IsOpen ? Storage.Current.StorageContainer : BackpackInventory.Instance;
	}

	private InventoryContainer GetHotbarTransferTarget( InventoryItem item )
	{
		return Storage.Current.IsOpen ? Storage.Current.StorageContainer : BackpackInventory.Instance;
	}

	private void GiveInitialItems()
	{
		TryGiveWeapon<CrossbowItem>();
		TryGiveAmmo( AmmoType.Bolt, 10 );

		var armor = ArmorItem.FromResource( "baseball_cap" );
		TryGiveItem( armor );

		var toolbox = InventorySystem.CreateItem<ToolboxItem>();
		TryGiveItem( toolbox );

		var crate = InventorySystem.CreateItem<CrateItem>();
		TryGiveItem( crate );

		crate = InventorySystem.CreateItem<CrateItem>();
		TryGiveItem( crate );

		crate = InventorySystem.CreateItem<CrateItem>();
		TryGiveItem( crate );
	}

	private void CreateInventories()
	{
		var hotbar = new InventoryContainer( this );
		hotbar.SetSlotLimit( 8 );
		hotbar.AddConnection( Client );
		hotbar.OnItemTaken += OnHotbarItemTaken;
		hotbar.OnItemGiven += OnHotbarItemGiven;
		InventorySystem.Register( hotbar );

		HotbarInventory = new NetInventoryContainer( hotbar );

		var backpack = new InventoryContainer( this );
		backpack.SetSlotLimit( 24 );
		backpack.AddConnection( Client );
		backpack.OnItemTaken += OnBackpackItemTaken;
		backpack.OnItemGiven += OnBackpackItemGiven;
		InventorySystem.Register( backpack );

		BackpackInventory = new NetInventoryContainer( backpack );

		var equipment = new InventoryContainer( this );
		equipment.SetSlotLimit( 3 );
		equipment.AddConnection( Client );
		equipment.OnItemTaken += OnEquipmentItemTaken;
		equipment.OnItemGiven += OnEquipmentItemGiven;
		equipment.SetGiveCondition( CanGiveEquipmentItem );
		InventorySystem.Register( equipment );

		EquipmentInventory = new NetInventoryContainer( equipment );
	}

	private void InitializeHotbarWeapons()
	{
		foreach ( var item in HotbarInventory.Instance.ItemList )
		{
			if ( item is WeaponItem weapon )
			{
				InitializeWeaponItem( weapon );
			}
		}
	}

	private void InitializeWeaponItem( WeaponItem item )
	{
		if ( !item.Weapon.IsValid() )
		{
			try
			{
				item.Weapon = TypeLibrary.Create<Weapon>( item.WeaponName );
				item.Weapon.SetWeaponItem( item );
				item.Weapon.OnCarryStart( this );
				item.IsDirty = true;
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}
		}
	}

	private void SimulateHotbar()
	{
		var currentSlotIndex = (int)CurrentHotbarIndex;

		if ( Input.MouseWheel > 0 )
			currentSlotIndex++;
		else if ( Input.MouseWheel < 0 )
			currentSlotIndex--;

		var maxSlotIndex = HotbarInventory.Instance.SlotLimit - 1;

		if ( currentSlotIndex < 0 )
			currentSlotIndex = maxSlotIndex;
		else if ( currentSlotIndex > maxSlotIndex )
			currentSlotIndex = 0;

		CurrentHotbarIndex = (ushort)currentSlotIndex;
		UpdateHotbarSlotKeys();

		if ( GetActiveHotbarItem() is WeaponItem weapon )
			ActiveChild = weapon.Weapon;
		else
			ActiveChild = null;
	}

	private void SimulateInventory()
	{
		var trace = Trace.Ray( CameraPosition, CameraPosition + CursorDirection * 1000f )
			.WorldOnly()
			.Run();

		if ( IsServer )
		{
			if ( Input.Released( InputButton.Drop ) )
			{
				var container = HotbarInventory.Instance;
				var item = container.GetFromSlot( CurrentHotbarIndex );

				if ( item.IsValid() )
				{
					var itemToDrop = item;

					if ( item.StackSize > 1 )
					{
						itemToDrop = InventorySystem.CreateDuplicateItem( item );
						itemToDrop.StackSize = 1;
						item.StackSize--;
					}

					var entity = new ItemEntity();
					entity.Position = trace.EndPosition;
					entity.SetItem( itemToDrop );
					entity.ApplyLocalImpulse( EyeRotation.Forward * 100f + Vector3.Up * 50f );
				}

				PlaySound( "item.dropped" );
			}
		}
		else if ( Prediction.FirstTime )
		{
			if ( Input.Pressed( InputButton.Score ) )
			{
				if ( !Backpack.Current.IsOpen )
					TimeSinceBackpackOpen = 0f;
				else
					IsBackpackToggleMode = false;

				if ( IDialog.IsActive() )
					IDialog.CloseActive();
				else
					Backpack.Current?.Open();
			}

			if ( Input.Released( InputButton.Score ) )
			{
				if ( TimeSinceBackpackOpen <= 0.2f )
				{
					IsBackpackToggleMode = true;
				}

				if ( !IsBackpackToggleMode )
				{
					Backpack.Current?.Close();
				}
			}
		}
	}

	private void UpdateHotbarSlotKeys()
	{
		var index = CurrentHotbarIndex;

		if ( Input.Pressed( InputButton.Slot1 ) )
			index = (ushort)Math.Min( 0, HotbarInventory.Instance.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot2 ) )
			index = (ushort)Math.Min( 1, HotbarInventory.Instance.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot3 ) )
			index = (ushort)Math.Min( 2, HotbarInventory.Instance.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot4 ) )
			index = (ushort)Math.Min( 3, HotbarInventory.Instance.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot5 ) )
			index = (ushort)Math.Min( 4, HotbarInventory.Instance.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot6 ) )
			index = (ushort)Math.Min( 5, HotbarInventory.Instance.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot7 ) )
			index = (ushort)Math.Min( 6, HotbarInventory.Instance.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot8 ) )
			index = (ushort)Math.Min( 7, HotbarInventory.Instance.SlotLimit - 1 );

		if ( index != CurrentHotbarIndex )
		{
			var container = HotbarInventory.Instance;
			var item = container.GetFromSlot( index );

			if ( item is IConsumableItem consumable )
			{
				if ( IsServer )
				{
					consumable.Consume( this );
				}

				return;
			}

			CurrentHotbarIndex = index;
		}
	}
}
