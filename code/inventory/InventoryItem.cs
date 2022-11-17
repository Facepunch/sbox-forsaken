using Sandbox;
using System.IO;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class InventoryItem : IValid
{
	public InventoryContainer Container { get; set; }
	public ItemEntity WorldEntity { get; private set; }
	public bool IsWorldEntity { get; private set; }

	public virtual ushort DefaultStackSize => 1;
	public virtual ushort MaxStackSize => 1;
	public virtual string WorldModel => "models/sbox_props/burger_box/burger_box.vmdl";
	public virtual string Description => string.Empty;
	public virtual bool DropOnDeath => false;
	public virtual Color Color => Color.White;
	public virtual string Name => string.Empty;
	public virtual Color IconTintColor => Color.White;
	public virtual string UniqueId => string.Empty;
	public virtual string Icon => string.Empty;

	public virtual Dictionary<string, int> RequiredItems => null;
	public virtual bool IsCraftable => false;

	private ItemTag[] InternalTags;

	public ItemTag[] Tags
	{
		get
		{
			if ( InternalTags == null )
			{
				var tags = new List<ItemTag>();
				BuildTags( tags );
				InternalTags = tags.ToArray();
			}

			return InternalTags;
		}
	}

	public static InventoryItem Deserialize( byte[] data )
	{
		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				return reader.ReadInventoryItem();
			}
		}
	}

	public byte[] Serialize()
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.WriteInventoryItem( this );
				return stream.ToArray();
			}
		}
	}

	private ushort InternalStackSize;
	private bool InternalIsDirty;

	public ushort StackSize
	{
		get => InternalStackSize;

		set
		{
			if ( InternalStackSize != value )
			{
				InternalStackSize = value;
				IsDirty = true;
			}
		}
	}

	public bool IsServer => Host.IsServer;
	public bool IsClient => Host.IsClient;
	public bool IsInstance => ItemId > 0;

	public bool IsDirty
	{
		get => InternalIsDirty;

		set
		{
			if ( IsServer )
			{
				if ( Container == null )
				{
					InternalIsDirty = false;
					return;
				}

				InternalIsDirty = value;

				if ( InternalIsDirty )
				{
					Container.IsDirty = true;
				}
			}
		}
	}

	public bool IsValid { get; set; }
	public ulong ItemId { get; set; }
	public ushort SlotId { get; set; }

	public void SetWorldEntity( ItemEntity entity )
	{
		WorldEntity = entity;
		IsWorldEntity = entity.IsValid();
		IsDirty = true;
		Remove();
	}

	public void ClearWorldEntity()
	{
		WorldEntity = null;
		IsWorldEntity = false;
		IsDirty = true;
	}

	public void Remove()
	{
		if ( Container.IsValid() )
		{
			Container.Remove( this );
		}
	}

	public void Replace( InventoryItem other )
	{
		if ( Container.IsValid() )
		{
			Container.Replace( SlotId, other );
		}
	}

	public virtual void BuildTags( List<ItemTag> tags )
	{
		
	}

	public virtual bool IsSameType( InventoryItem other )
	{
		return (GetType() == other.GetType());
	}

	public virtual bool CanStackWith( InventoryItem other )
	{
		return true;
	}

	public virtual void Write( BinaryWriter writer )
	{
		if ( WorldEntity.IsValid() )
		{
			writer.Write( true );
			writer.Write( WorldEntity.NetworkIdent );
		}
		else
		{
			writer.Write( false );
		}

	}

	public virtual void Read( BinaryReader reader )
	{
		IsWorldEntity = reader.ReadBoolean();

		if ( IsWorldEntity )
		{
			WorldEntity = (Entity.FindByIndex( reader.ReadInt32() ) as ItemEntity);
		}
	}

	public virtual void OnRemoved()
	{

	}

	public virtual void OnCreated()
	{

	}
}
