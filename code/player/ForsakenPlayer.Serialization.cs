using Sandbox;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer
{
	private PersistenceHandle BedrollHandle { get; set; }

	public virtual bool ShouldSaveState()
	{
		return true;
	}

	public virtual void BeforeStateLoaded()
	{

	}

	public virtual void AfterStateLoaded()
	{
		if ( BedrollHandle.IsValid() )
		{
			Bedroll = All.OfType<Bedroll>().FirstOrDefault( e => e.Handle == BedrollHandle );
		}
	}

	public virtual void SerializeState( BinaryWriter writer )
	{
		writer.Write( SteamId );
		writer.Write( (byte)LifeState );
		writer.Write( Transform );

		writer.Write( Health );
		writer.Write( Stamina );
		writer.Write( Calories );
		writer.Write( Hydration );

		SerializeCraftingQueue( writer );

		writer.Write( Hotbar );
		writer.Write( Backpack );
		writer.Write( Equipment );

		if ( Bedroll.IsValid() )
		{
			writer.Write( true );
			writer.Write( Bedroll.Handle );
		}
		else
		{
			writer.Write( false );
		}
	}

	public virtual void DeserializeState( BinaryReader reader )
	{
		SteamId = reader.ReadInt64();
		LifeState = (LifeState)reader.ReadByte();
		Transform = reader.ReadTransform();

		Health = reader.ReadSingle();
		Stamina = reader.ReadSingle();
		Calories = reader.ReadSingle();
		Hydration = reader.ReadSingle();

		DeserializeCraftingQueue( reader );

		var hotbar = reader.ReadInventoryContainer();
		hotbar.SetEntity( this );
		hotbar.AddConnection( Client );
		hotbar.ItemTaken += OnHotbarItemTaken;
		hotbar.ItemGiven += OnHotbarItemGiven;
		InternalHotbar = new NetInventoryContainer( hotbar );

		var backpack = reader.ReadInventoryContainer();
		backpack.SetEntity( this );
		backpack.AddConnection( Client );
		backpack.ItemTaken += OnBackpackItemTaken;
		backpack.ItemGiven += OnBackpackItemGiven;
		InternalBackpack = new NetInventoryContainer( backpack );

		var equipment = reader.ReadInventoryContainer();
		equipment.SetEntity( this );
		equipment.AddConnection( Client );
		equipment.ItemTaken += OnEquipmentItemTaken;
		equipment.ItemGiven += OnEquipmentItemGiven;
		InternalEquipment = new NetInventoryContainer( equipment );

		if ( reader.ReadBoolean() )
		{
			BedrollHandle = reader.ReadPersistenceHandle();
		}
	}

	private void SerializeCraftingQueue( BinaryWriter writer )
	{
		var count = CraftingQueue.Count;
		writer.Write( count );

		for ( var i = 0; i < count; i++ )
		{
			var entry = CraftingQueue[i];
			writer.Write( entry.Recipe.ResourceId );
			writer.Write( entry.Quantity );
		}
	}

	private void DeserializeCraftingQueue( BinaryReader reader )
	{
		CraftingQueue.Clear();

		var count = reader.ReadInt32();

		for ( var i = 0; i < count; i++ )
		{
			var resourceId = reader.ReadInt32();
			var quantity = reader.ReadInt32();
			var recipe = ResourceLibrary.Get<RecipeResource>( resourceId );

			if ( recipe is not null )
			{
				var entry = new CraftingQueueEntry
				{
					ResourceId = recipe.ResourceId,
					Quantity = quantity
				};

				if ( CraftingQueue.Count == 0 )
				{
					entry.FinishTime = recipe.CraftingTime;
				}

				CraftingQueue.Add( entry );
			}
		}
	}
}
