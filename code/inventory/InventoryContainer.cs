using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public class InventoryContainer : IValid
{
	public delegate void ItemTakenCallback( ushort slot, InventoryItem instance );
	public delegate void ItemGivenCallback( ushort slot, InventoryItem instance );
	public delegate void SlotChangedCallback( ushort slot );
	public delegate bool GiveConditionCallback( ushort slot, InventoryItem instance );
	public delegate bool TakeConditionCallback( ushort slot, InventoryItem instance );
	public delegate InventoryContainer TransferHandlerCallback( InventoryItem instance );

	public event SlotChangedCallback OnSlotChanged;
	public event SlotChangedCallback OnDataChanged;
	public event ItemGivenCallback OnItemGiven;
	public event ItemTakenCallback OnItemTaken;
	public event Action<Client> OnConnectionRemoved;
	public event Action<Client> OnConnectionAdded;

	private bool InternalIsDirty;

	public bool IsServer => Host.IsServer;
	public bool IsClient => Host.IsClient;

	public bool IsDirty
	{
		get => InternalIsDirty;

		set
		{
			if ( IsServer )
			{
				if ( InternalIsDirty != value )
				{
					InternalIsDirty = value;

					if ( InternalIsDirty )
					{
						InventorySystem.AddToDirtyList( this );
					}
				}
			}
		}
	}

	public TransferHandlerCallback TransferHandler { get; private set; }
	public GiveConditionCallback GiveCondition { get; private set; }
	public TakeConditionCallback TakeCondition { get; private set; }
	public ulong InventoryId { get; private set; }
	public Entity Entity { get; }
	public List<Client> Connections { get; }
	public List<InventoryItem> ItemList { get; }
	public ushort SlotLimit { get; private set; }
	public bool IsEmpty => !ItemList.Any( i => i.IsValid() );

	public bool IsValid => true;

	public static InventoryContainer Deserialize( byte[] data )
	{
		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				return reader.ReadInventoryContainer();
			}
		}
	}

	public InventoryContainer( Entity owner )
	{
		ItemList = new List<InventoryItem>();
		Connections = new List<Client>();
		Entity = owner;
	}

	public byte[] Serialize()
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.WriteInventoryContainer( this );
				return stream.ToArray();
			}
		}
	}

	public void InvokeDataChanged( ushort slot )
	{
		OnDataChanged?.Invoke( slot );
	}

	public void SetTransferHandler( TransferHandlerCallback callback )
	{
		TransferHandler = callback;
	}

	public void SetGiveCondition( GiveConditionCallback condition )
	{
		GiveCondition = condition;
	}

	public void SetTakeCondition( TakeConditionCallback condition )
	{
		TakeCondition = condition;
	}

	public bool IsOccupied( ushort slot )
	{
		return (GetFromSlot( slot ) != null);
	}

	public bool SetSlotLimit( ushort slotLimit )
	{
		if ( slotLimit >= ItemList.Count )
		{
			var difference = slotLimit - ItemList.Count;

			for ( var i = 0; i < difference; i++ )
			{
				ItemList.Add( null );
			}
		}
		else if ( slotLimit < ItemList.Count )
		{
			return false;
		}

		SlotLimit = slotLimit;

		return true;
	}

	public InventoryItem GetItem( ulong itemId )
	{
		if ( itemId == 0 )
		{
			return null;
		}

		for ( int i = 0; i < ItemList.Count; i++ )
		{
			var instance = ItemList[i];

			if ( instance != null && instance.ItemId == itemId )
			{
				return ItemList[i];
			}
		}

		return null;
	}

	public void AddConnection( Client connection )
	{
		if ( !Connections.Contains( connection ) )
		{
			Connections.Add( connection );
			OnConnectionAdded?.Invoke( connection );
		}
	}

	public void RemoveConnection( Client connection )
	{
		if ( Connections.Contains( connection ) )
		{
			Connections.Remove( connection );
			OnConnectionRemoved?.Invoke( connection );
		}
	}

	public bool IsConnected( Client connection )
	{
		return Connections.Contains( connection );
	}

	public InventoryItem GetFromSlot( ushort slot )
	{
		return ItemList[slot];
	}

	public void SetInventoryId( ulong inventoryId )
	{
		InventoryId = inventoryId;
	}

	public IEnumerable<Client> GetRecipients()
	{
		return Client.All
			.Where( c => c.Components.TryGet<InventoryViewer>( out var viewer ) && viewer.ContainerId == InventoryId )
			.Concat( Connections )
			.Distinct();
	}

	public void SendDirtyItems()
	{
		var recipients = GetRecipients();

		if ( recipients.Any() )
		{
			InventorySystem.SendDirtyItemsEvent( To.Multiple( recipients ), this );
		}
	}

	public List<InventoryItem> FindItems( Type type )
	{
		var output = new List<InventoryItem>();

		for ( int i = 0; i < ItemList.Count; i++ )
		{
			var item = ItemList[i];

			if ( item.IsValid() && item.GetType().IsAssignableTo( type ) )
			{
				output.Add( item );
			}
		}

		return output;
	}

	public List<T> FindItems<T>() where T : InventoryItem
	{
		var output = new List<T>();

		for ( int i = 0; i < ItemList.Count; i++ )
		{
			var instance = (ItemList[i] as T);

			if ( instance != null )
			{
				output.Add( instance );
			}
		}

		return output;
	}

	public bool Split( InventoryContainer target, ushort fromSlot, ushort toSlot )
	{
		if ( !IsOccupied( fromSlot ) )
		{
			return false;
		}

		var item = GetFromSlot( fromSlot );

		if ( TakeCondition != null && !TakeCondition( fromSlot, item ) )
			return false;

		if ( target.GiveCondition != null && !target.GiveCondition( toSlot, item ) )
			return false;

		if ( IsClient )
		{
			InventorySystem.SendSplitEvent( this, target, fromSlot, toSlot );
			return true;
		}

		if ( item.StackSize == 1 )
		{
			return Move( target, fromSlot, toSlot );
		}

		var splitStackSize = item.StackSize / 2;

		if ( item.StackSize - splitStackSize <= 0 )
		{
			return Move( target, fromSlot, toSlot );
		}

		var splitItem = InventorySystem.DuplicateItem( item );
		splitItem.StackSize = (ushort)splitStackSize;

		var targetItem = target.GetFromSlot( toSlot );

		if ( targetItem.IsValid() )
		{
			var remaining = target.Stack( splitItem, toSlot );
			splitStackSize -= remaining;
			item.StackSize -= (ushort)splitStackSize;
		}
		else
		{
			target.Give( splitItem, toSlot );
			item.StackSize -= (ushort)splitStackSize;
		}

		return true;
	}

	public bool Move( InventoryContainer target, ushort fromSlot, ushort toSlot )
	{
		if ( !IsOccupied( fromSlot ) )
		{
			return false;
		}

		var fromInstance = ItemList[fromSlot];

		if ( TakeCondition != null && !TakeCondition( fromSlot, fromInstance ) )
			return false;

		if ( target.GiveCondition != null && !target.GiveCondition( toSlot, fromInstance ) )
			return false;

		if ( IsClient )
		{
			InventorySystem.SendMoveEvent( this, target, fromSlot, toSlot );
			return true;
		}

		if ( target.IsOccupied( toSlot ) )
		{
			var toInstance = target.ItemList[toSlot];
			var canStack = fromInstance.IsSameType( toInstance ) && fromInstance.CanStackWith( toInstance );

			if ( canStack )
			{
				ushort amountCanStack = (ushort)Math.Max( toInstance.MaxStackSize - toInstance.StackSize, 0 );
				ushort amountToTake = 0;

				if ( amountCanStack > 0 )
				{
					amountToTake = fromInstance.StackSize;

					if ( amountCanStack < amountToTake )
						amountToTake = amountCanStack;

					if ( amountToTake >= 0 )
						toInstance.StackSize += amountToTake;

					if ( amountToTake >= fromInstance.StackSize )
					{
						fromInstance.StackSize = 0;
						ClearSlot( fromSlot );
					}
					else
					{
						fromInstance.StackSize -= amountCanStack;
					}
				}

				if ( amountToTake > 0 )
				{
					return true;
				}
			}

			fromInstance.Container = target;
			fromInstance.SlotId = toSlot;

			toInstance.Container = this;
			toInstance.SlotId = fromSlot;

			SendTakeEvent( fromSlot, fromInstance );
			target.SendTakeEvent( toSlot, toInstance );

			target.ItemList[toSlot] = fromInstance;
			target.SendGiveEvent( toSlot, fromInstance );

			ItemList[fromSlot] = toInstance;
			SendGiveEvent( fromSlot, toInstance );
		}
		else
		{
			fromInstance.SlotId = toSlot;
			fromInstance.Container = target;

			target.ItemList[toSlot] = fromInstance;
			target.SendGiveEvent( toSlot, fromInstance );

			ClearSlot( fromSlot, false );
		}

		return true;
	}

	public bool Is( InventoryContainer container )
	{
		return container == this;
	}

	public bool Is( NetInventoryContainer container )
	{
		return container.Value == this;
	}

	public InventoryItem Remove( InventoryItem item )
	{
		return Remove( item.ItemId );
	}

	public InventoryItem Remove( ulong itemId )
	{
		if ( itemId == 0 )
		{
			return null;
		}

		for ( ushort i = 0; i < ItemList.Count; i++ )
		{
			var instance = ItemList[i];

			if ( instance != null && instance.ItemId == itemId )
			{
				return ClearSlot( i );
			}
		}

		return null;
	}

	public InventoryItem ClearSlot( ushort slot, bool clearItemContainer = true )
	{
		if ( IsClient )
		{
			return null;
		}

		if ( !IsOccupied( slot ) )
		{
			return null;
		}

		var instance = GetFromSlot( slot );

		if ( clearItemContainer )
		{
			if ( instance.Container == this )
			{
				instance.Container = null;
				instance.SlotId = 0;
			}
		}

		ItemList[slot] = null;

		SendTakeEvent( slot, instance );

		return instance;
	}

	public List<InventoryItem> Give( List<InventoryItem> instances )
	{
		var remainder = new List<InventoryItem>();

		for ( var i = 0; i < instances.Count; i++ )
		{
			var instance = instances[i];

			if ( !Give( instance ) )
			{
				remainder.Add( instance );
			}
		}

		return remainder;
	}

	public bool FindFreeSlot( out ushort slot )
	{
		var slotLimit = SlotLimit;

		for ( ushort i = 0; i < slotLimit; i++ )
		{
			if ( ItemList[i] == null )
			{
				slot = i;
				return true;
			}
		}

		slot = 0;
		return false;
	}

	public bool Give( InventoryItem instance )
	{
		if ( !FindFreeSlot( out var slot ) )
		{
			return false;
		}

		return Give( instance, slot );
	}

	public InventoryItem Replace( ushort slot, InventoryItem instance )
	{
		if ( IsClient )
		{
			return null;
		}

		var slotLimit = SlotLimit;

		if ( slot >= slotLimit )
		{
			Log.Info( "Unable to give an item to this inventory because slot #" + slot + " is greater than the limit of " + slotLimit );
			return null;
		}

		var oldItem = ItemList[slot];

		if ( oldItem.IsValid() )
		{
			oldItem.Container = null;
			oldItem.SlotId = 0;
		}

		instance.SlotId = slot;
		instance.Container = this;

		ItemList[slot] = instance;

		SendGiveEvent( slot, instance );

		return oldItem;
	}

	public bool Give( InventoryItem instance, ushort slot )
	{
		if ( IsClient )
		{
			return false;
		}

		var slotLimit = SlotLimit;

		if ( slot >= slotLimit )
		{
			Log.Info( "Unable to give an item to this inventory because slot #" + slot + " is greater than the limit of " + slotLimit );
			return false;
		}

		if ( ItemList[slot] != null )
		{
			Log.Info( "Unable to give an item to this inventory because slot #" + slot + " is occupied!" );
			return false;
		}

		instance.SlotId = slot;
		instance.Container = this;

		ItemList[slot] = instance;

		SendGiveEvent( slot, instance );

		return true;
	}

	public ushort Stack( InventoryItem instance, ushort slot )
	{
		var amount = instance.StackSize;
		var item = ItemList[slot];

		if ( GiveCondition != null && !GiveCondition( slot, item ) )
			return amount;

		if ( item != null && item.IsSameType( instance ) && item.CanStackWith( instance ) )
		{
			var amountCanStack = (ushort)Math.Max( item.MaxStackSize - item.StackSize, 0 );

			if ( amountCanStack >= amount )
			{
				item.StackSize += amount;
				amount = 0;
			}
			else
			{
				item.StackSize += amountCanStack;
				amount = (ushort)Math.Max( amount - amountCanStack, 0 );
			}

			if ( amount == 0 ) return 0;
		}

		if ( amount > 0 )
		{
			if ( Give( instance, slot ) )
			{
				instance.StackSize = amount;
				return 0;
			}
		}

		return amount;
	}

	public ushort Stack( InventoryItem instance )
	{
		var amount = instance.StackSize;

		for ( int i = 0; i < ItemList.Count; i++ )
		{
			var item = ItemList[i];

			if ( GiveCondition != null && !GiveCondition( (ushort)i, instance ) )
				continue;

			if ( item != null && item.IsSameType( instance ) && item.CanStackWith( instance ) )
			{
				var amountCanStack = (ushort)Math.Max( item.MaxStackSize - item.StackSize, 0 );

				if ( amountCanStack >= amount )
				{
					item.StackSize += amount;
					amount = 0;
				}
				else
				{
					item.StackSize += amountCanStack;
					amount = (ushort)Math.Max( amount - amountCanStack, 0 );
				}

				if ( amount == 0 ) return 0;
			}
		}

		if ( amount > 0 )
		{
			var success = Give( instance );

			if ( success )
			{
				instance.StackSize = amount;
				return 0;
			}
		}

		return amount;
	}

	public List<InventoryItem> RemoveAll()
	{
		var output = new List<InventoryItem>();

		for ( ushort i = 0; i < ItemList.Count; i++ )
		{
			var instance = ClearSlot( i );

			if ( instance != null )
			{
				output.Add( instance );
			}
		}

		return output;
	}

	private void SendGiveEvent( ushort slot, InventoryItem instance )
	{
		if ( IsClient )
		{
			return;
		}

		var recipients = GetRecipients();

		if ( recipients.Any() )
		{
			InventorySystem.SendGiveItemEvent( To.Multiple( recipients ), this, slot, instance );
		}

		HandleSlotChanged( slot );

		OnItemGiven?.Invoke( slot, instance );
	}

	private void SendTakeEvent( ushort slot, InventoryItem instance )
	{
		if ( IsClient )
		{
			return;
		}

		var recipients = GetRecipients();

		if ( recipients.Any() )
		{
			InventorySystem.SendTakeItemEvent( To.Multiple( recipients ), this, slot );
		}

		HandleSlotChanged( slot );

		if ( instance != null )
		{
			OnItemTaken?.Invoke( slot, instance );
		}
	}

	private void HandleSlotChanged( ushort slot )
	{
		OnSlotChanged?.Invoke( slot );
	}

	public void ProcessGiveItemEvent( BinaryReader reader )
	{
		var instance = reader.ReadInventoryItem();
		var slot = reader.ReadUInt16();

		instance.Container = this;
		instance.SlotId = slot;

		ItemList[slot] = instance;
		HandleSlotChanged( slot );
		OnItemGiven?.Invoke( slot, instance );
	}

	public void ProcessTakeItemEvent( BinaryReader reader )
	{
		var slot = reader.ReadUInt16();
		var instance = ItemList[slot];

		if ( instance != null )
		{
			if ( instance.Container == this && instance.SlotId == slot )
			{
				instance.Container = null;
				instance.SlotId = 0;
			}

			ItemList[slot] = null;
			HandleSlotChanged( slot );
			OnItemTaken?.Invoke( slot, instance );
		}
	}

	public override int GetHashCode()
	{
		var hash = 19;

		foreach ( var item in ItemList )
		{
			if ( item.IsValid() )
				hash = hash * 31 + item.GetHashCode();
			else
				hash = hash * 31 + 0;
		}

		return HashCode.Combine( hash, SlotLimit );
	}
}
