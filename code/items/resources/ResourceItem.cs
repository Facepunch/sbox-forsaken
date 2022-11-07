using Sandbox;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public class ResourceItem<A,T> : InventoryItem where A : ItemResource where T : ResourceItem<A,T>
{
	public static T FromResource( string assetName )
	{
		var asset = ResourceLibrary.GetAll<A>()
			.Where( a => a.ResourceName.ToLower() == assetName )
			.FirstOrDefault();

		if ( asset != null )
		{
			var item = InventorySystem.CreateItem<T>();
			item.Resource = asset;
			return item;
		}

		return null;
	}

	public override string Name => Resource.ItemName;
	public override string Description => Resource.Description;
	public override string Icon => Resource.Icon;

	public A Resource { get; set; }

	public override void Read( BinaryReader reader )
	{
		var id = reader.ReadInt32();
		Resource = ResourceLibrary.Get<A>( id );
		base.Read( reader );
	}

	public override void Write( BinaryWriter writer )
	{
		writer.Write( Resource.ResourceId );
		base.Write( writer );
	}
}
