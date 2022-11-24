using Sandbox;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer
{
	[ConCmd.Server( "fsk.item.throw" )]
	private static void ThrowItemCmd( ulong itemId, string directionCsv )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player )
			return;

		var split = directionCsv.Split( ',' );
		var direction = new Vector3( split[0].ToFloat(), split[1].ToFloat(), 0f );
		var item = InventorySystem.FindInstance( itemId );

		if ( item.IsValid() )
		{
			var entity = new ItemEntity();

			entity.Position = player.EyePosition + direction * 10f;
			entity.SetItem( item );
			entity.ApplyLocalImpulse( direction * 300f + Vector3.Down * 10f );
		}
	}

	public static void ThrowItem( InventoryItem item, Vector3 direction )
	{
		if ( !item.IsValid() ) return;
		var csv = $"{direction.x},{direction.y}";
		ThrowItemCmd( item.ItemId, csv );
	}

	public IEnumerable<T> FindItems<T>() where T : InventoryItem
	{
		foreach ( var item in Hotbar.FindItems<T>() ) yield return item;
		foreach ( var item in Backpack.FindItems<T>() ) yield return item;
		foreach ( var item in Equipment.FindItems<T>() ) yield return item;
	}

	public IEnumerable<InventoryItem> FindItems( Type type )
	{
		foreach ( var item in Hotbar.FindItems( type ) ) yield return item;
		foreach ( var item in Backpack.FindItems( type ) ) yield return item;
		foreach ( var item in Equipment.FindItems( type ) ) yield return item;
	}

	public ushort TakeAmmo( AmmoType type, ushort count )
	{
		var items = new List<AmmoItem>();

		items.AddRange( Hotbar.FindItems<AmmoItem>() );
		items.AddRange( Backpack.FindItems<AmmoItem>() );

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

		items.AddRange( Hotbar.FindItems<AmmoItem>() );
		items.AddRange( Backpack.FindItems<AmmoItem>() );

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

	public int GetItemCount( string uniqueId )
	{
		var totalItems = 0;

		totalItems += Hotbar.FindItems<InventoryItem>().Where( i => i.UniqueId == uniqueId ).Sum( i => i.StackSize );
		totalItems += Backpack.FindItems<InventoryItem>().Where( i => i.UniqueId == uniqueId ).Sum( i => i.StackSize );

		return totalItems;
	}

	public bool HasItems( string uniqueId, int count )
	{
		return (GetItemCount( uniqueId ) >= count);
	}

	public int TakeItems( string uniqueId, int count )
	{
		var items = new List<InventoryItem>();

		items.AddRange( Hotbar.FindItems<InventoryItem>().Where( i => i.UniqueId == uniqueId ) );
		items.AddRange( Backpack.FindItems<InventoryItem>().Where( i => i.UniqueId == uniqueId ) );

		var amountLeftToTake = count;
		var totalAmountTaken = 0;

		for ( int i = items.Count - 1; i >= 0; i-- )
		{
			var item = items[i];

			if ( item.StackSize >= amountLeftToTake )
			{
				item.StackSize -= (ushort)amountLeftToTake;
				totalAmountTaken += amountLeftToTake;
				amountLeftToTake = 0;
			}
			else
			{
				amountLeftToTake -= item.StackSize;
				totalAmountTaken += item.StackSize;
				item.StackSize = 0;
			}

			if ( item.StackSize <= 0 )
				item.Remove();

			if ( amountLeftToTake <= 0 )
				break;
		}

		return totalAmountTaken;
	}

	public bool TryGiveArmor( ArmorItem item )
	{
		var slotToIndex = (int)item.ArmorSlot - 1;
		return Equipment.Give( item, (ushort)slotToIndex );
	}

	public ushort TryGiveItem( InventoryItem item )
	{
		var remaining = Hotbar.Stack( item );

		if ( remaining > 0 )
		{
			remaining = Backpack.Stack( item );
		}

		return remaining;
	}

	public bool TryGiveWeapon( WeaponItem item )
	{
		if ( Hotbar.Give( item ) )
			return true;

		return Backpack.Give( item );
	}

	public void TryGiveAmmo( AmmoType type, ushort amount )
	{
		var resource = ResourceLibrary.GetAll<AmmoResource>()
			.Where( a => a.AmmoType == type )
			.FirstOrDefault();

		var item = InventorySystem.CreateItem<AmmoItem>( resource.UniqueId );
		item.StackSize = amount;

		var remaining = Hotbar.Stack( item );

		if ( remaining > 0 )
		{
			Backpack.Stack( item );
		}
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
		if ( instance is ArmorItem armor && !Equipment.Is( instance.Parent ) )
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
			InitializeWeapon( weapon );
		}
	}

	private void OnHotbarItemTaken( ushort slot, InventoryItem instance )
	{
		if ( instance is WeaponItem weapon )
		{
			if ( weapon.Weapon.IsValid() && !Hotbar.Is( instance.Parent ) )
			{
				weapon.Weapon.Delete();
				weapon.Weapon = null;
				weapon.IsDirty = true;
			}
		}
	}

	private void GiveInitialItems()
	{
		var mp5 = InventorySystem.CreateItem<WeaponItem>( "mp5a4" );
		TryGiveWeapon( mp5 );

		var crossbow = InventorySystem.CreateItem<WeaponItem>( "crossbow" );
		TryGiveWeapon( crossbow );

		TryGiveAmmo( AmmoType.Pistol, 200 );
		TryGiveAmmo( AmmoType.Bolt, 10 );

		var armor = InventorySystem.CreateItem<ArmorItem>( "baseball_cap" );
		TryGiveItem( armor );

		armor = InventorySystem.CreateItem<ArmorItem>( "black_hoodie" );
		TryGiveItem( armor );

		armor = InventorySystem.CreateItem<ArmorItem>( "cargo_pants" );
		TryGiveItem( armor );

		armor = InventorySystem.CreateItem<ArmorItem>( "army_boots" );
		TryGiveItem( armor );

		var toolbox = InventorySystem.CreateItem<ToolboxItem>();
		TryGiveItem( toolbox );

		var crate = InventorySystem.CreateItem<StorageCrateItem>();
		TryGiveItem( crate );

		crate = InventorySystem.CreateItem<StorageCrateItem>();
		TryGiveItem( crate );

		crate = InventorySystem.CreateItem<StorageCrateItem>();
		TryGiveItem( crate );
	}

	private void CreateInventories()
	{
		var hotbar = new HotbarContainer();
		hotbar.SetEntity( this );
		hotbar.AddConnection( Client );
		hotbar.ItemTaken += OnHotbarItemTaken;
		hotbar.ItemGiven += OnHotbarItemGiven;
		InventorySystem.Register( hotbar );

		InternalHotbar = new NetInventoryContainer( hotbar );

		var backpack = new BackpackContainer();
		backpack.SetEntity( this );
		backpack.AddConnection( Client );
		backpack.ItemTaken += OnBackpackItemTaken;
		backpack.ItemGiven += OnBackpackItemGiven;
		InventorySystem.Register( backpack );

		InternalBackpack = new NetInventoryContainer( backpack );

		var equipment = new EquipmentContainer();
		equipment.SetEntity( this );
		equipment.AddConnection( Client );
		equipment.ItemTaken += OnEquipmentItemTaken;
		equipment.ItemGiven += OnEquipmentItemGiven;
		InventorySystem.Register( equipment );

		InternalEquipment = new NetInventoryContainer( equipment );
	}

	private void InitializeWeapons()
	{
		foreach ( var item in Hotbar.ItemList )
		{
			if ( item is WeaponItem weapon )
			{
				InitializeWeapon( weapon );
			}
		}
	}

	private void InitializeWeapon( WeaponItem item )
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
		var currentSlotIndex = (int)HotbarIndex;

		if ( Input.MouseWheel > 0 )
			currentSlotIndex++;
		else if ( Input.MouseWheel < 0 )
			currentSlotIndex--;

		var maxSlotIndex = Hotbar.SlotLimit - 1;

		if ( currentSlotIndex < 0 )
			currentSlotIndex = maxSlotIndex;
		else if ( currentSlotIndex > maxSlotIndex )
			currentSlotIndex = 0;

		HotbarIndex = (ushort)currentSlotIndex;
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
				var container = Hotbar;
				var item = container.GetFromSlot( HotbarIndex );

				if ( item.IsValid() )
				{
					var itemToDrop = item;

					if ( item.StackSize > 1 )
					{
						itemToDrop = InventorySystem.DuplicateItem( item );
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
				if ( !UI.Backpack.Current.IsOpen )
					TimeSinceBackpackOpen = 0f;
				else
					IsBackpackToggleMode = false;

				if ( UI.Dialog.IsActive() )
					UI.Dialog.Close();
				else
					UI.Backpack.Current?.Open();
			}

			if ( Input.Released( InputButton.Score ) )
			{
				if ( TimeSinceBackpackOpen <= 0.2f )
				{
					IsBackpackToggleMode = true;
				}

				if ( !IsBackpackToggleMode )
				{
					UI.Backpack.Current?.Close();
				}
			}
		}
	}

	private void UpdateHotbarSlotKeys()
	{
		var index = HotbarIndex;

		if ( Input.Pressed( InputButton.Slot1 ) )
			index = (ushort)Math.Min( 0, Hotbar.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot2 ) )
			index = (ushort)Math.Min( 1, Hotbar.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot3 ) )
			index = (ushort)Math.Min( 2, Hotbar.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot4 ) )
			index = (ushort)Math.Min( 3, Hotbar.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot5 ) )
			index = (ushort)Math.Min( 4, Hotbar.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot6 ) )
			index = (ushort)Math.Min( 5, Hotbar.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot7 ) )
			index = (ushort)Math.Min( 6, Hotbar.SlotLimit - 1 );

		if ( Input.Pressed( InputButton.Slot8 ) )
			index = (ushort)Math.Min( 7, Hotbar.SlotLimit - 1 );

		if ( index != HotbarIndex )
		{
			var container = Hotbar;
			var item = container.GetFromSlot( index );

			if ( item is IConsumableItem consumable )
			{
				if ( IsServer )
				{
					consumable.Consume( this );
				}

				return;
			}

			HotbarIndex = index;
		}
	}
}
