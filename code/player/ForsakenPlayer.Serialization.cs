using Sandbox;
using System.IO;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer
{
	public virtual bool ShouldPersist()
	{
		return true;
	}

	public virtual void Serialize( BinaryWriter writer )
	{
		writer.Write( Transform );

		writer.Write( Health );
		writer.Write( Stamina );
		writer.Write( Calories );
		writer.Write( Hydration );

		SerializeCraftingQueue( writer );

		writer.WriteInventoryContainer( Hotbar );
		writer.WriteInventoryContainer( Backpack );
		writer.WriteInventoryContainer( Equipment );
	}

	public virtual void Deserialize( BinaryReader reader )
	{
		Transform = reader.ReadTransform();

		Health = reader.ReadSingle();
		Stamina = reader.ReadSingle();
		Calories = reader.ReadSingle();
		Hydration = reader.ReadSingle();

		DeserializeCraftingQueue( reader );

		var hotbar = reader.ReadInventoryContainer();
		hotbar.SetEntity( this );
		hotbar.AddConnection( Client );
		InternalHotbar = new NetInventoryContainer( hotbar );

		var backpack = reader.ReadInventoryContainer();
		backpack.SetEntity( this );
		backpack.AddConnection( Client );
		InternalBackpack = new NetInventoryContainer( backpack );

		var equipment = reader.ReadInventoryContainer();
		equipment.SetEntity( this );
		equipment.AddConnection( Client );
		InternalEquipment = new NetInventoryContainer( equipment );
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
