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
		writer.Write( TimeSinceLastKilled.Relative );
		writer.Write( SteamId );
		writer.Write( DisplayName );
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

		writer.Write( Markers.Count );

		foreach ( var marker in Markers )
		{
			writer.Write( marker.Position );
			writer.Write( marker.Color );
		}

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
		TimeSinceLastKilled = reader.ReadSingle();
		SteamId = reader.ReadInt64();
		DisplayName = reader.ReadString();
		LifeState = (LifeState)reader.ReadByte();
		Transform = reader.ReadTransform();

		Health = reader.ReadSingle();
		Stamina = reader.ReadSingle();
		Calories = reader.ReadSingle();
		Hydration = reader.ReadSingle();

		DeserializeCraftingQueue( reader );

		var hotbar = reader.ReadInventoryContainer( Hotbar );
		InternalHotbar = new NetInventoryContainer( hotbar );

		var backpack = reader.ReadInventoryContainer( Backpack );
		InternalBackpack = new NetInventoryContainer( backpack );

		var equipment = reader.ReadInventoryContainer( Equipment );
		InternalEquipment = new NetInventoryContainer( equipment );

		Markers.Clear();

		var markerCount = reader.ReadInt32();
		for ( var i = 0; i < markerCount; i++ )
		{
			var position = reader.ReadVector3();
			var color = reader.ReadColor();
			Markers.Add( new MapMarker
			{
				Position = position,
				Color = color
			} );
		}

		if ( reader.ReadBoolean() )
		{
			BedrollHandle = reader.ReadPersistenceHandle();
		}

		if ( LifeState == LifeState.Alive )
			CreateHull();
		else
			Respawn();
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
