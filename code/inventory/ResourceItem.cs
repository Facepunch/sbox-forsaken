using System.Linq;

namespace Facepunch.Forsaken;

public class ResourceItem<A,T> : InventoryItem, IResourceItem where A : ItemResource where T : ResourceItem<A,T>
{
	public static T FromResource( string uniqueId )
	{
		var resource = ResourceLibrary.GetAll<A>()
			.Where( a => a.UniqueId.ToLower() == uniqueId )
			.FirstOrDefault();

		if ( resource != null )
		{
			var item = InventorySystem.CreateItem<T>();
			item.LoadResource( resource );
			return item;
		}

		return null;
	}

	public override string Name => Resource?.ItemName ?? string.Empty;
	public override string Description => Resource?.Description ?? string.Empty;
	public override string WorldModel => Resource?.WorldModel ?? string.Empty;
	public override string UniqueId => Resource?.UniqueId ?? string.Empty;
	public override string Icon => Resource?.Icon ?? string.Empty;

	public A Resource { get; set; }

	ItemResource IResourceItem.Resource => Resource;

	public void LoadResource( ItemResource resource )
	{
		if ( resource is not A )
		{
			Log.Error( $"Unable to load resource for { GetType().Name }. Resource is not of type: {typeof( A ).Name}!" );
			return;
		}

		Resource = resource as A;
	}
}
