using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public class ResourceItem<A,T> : InventoryItem where A : ItemResource where T : ResourceItem<A,T>
{
	public static T FromResource( string assetName )
	{
		var resource = ResourceLibrary.GetAll<A>()
			.Where( a => a.ResourceName.ToLower() == assetName )
			.FirstOrDefault();

		if ( resource != null )
		{
			var item = InventorySystem.CreateItem<T>();
			item.Resource = resource;
			return item;
		}

		return null;
	}

	public override Dictionary<string,int> RequiredItems => Resource?.RequiredItems ?? null;
	public override string Name => Resource?.ItemName ?? string.Empty;
	public override string Description => Resource?.Description ?? string.Empty;
	public override string WorldModel => Resource?.WorldModel ?? string.Empty;
	public override string Icon => Resource?.Icon ?? string.Empty;
	public override bool IsCraftable => Resource?.IsCraftable ?? false;

	public A Resource { get; set; }

	public override void Read( BinaryReader reader )
	{
		var id = reader.ReadInt32();

		Resource = ResourceLibrary.Get<A>( id );

		if ( Resource == null )
		{
			throw new Exception( $"Unable to locate the item game resource with id #{id}" );
		}

		base.Read( reader );
	}

	public override void Write( BinaryWriter writer )
	{
		writer.Write( Resource?.ResourceId ?? 0 );
		base.Write( writer );
	}
}
