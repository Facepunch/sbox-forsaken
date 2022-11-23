using System.Collections.Generic;

namespace Facepunch.Forsaken;

public struct ItemTag
{
	private static Dictionary<string, ItemTag> Tags { get; set; } = new();

	public static void Register( string id, string name, Color color )
	{
		if ( !Tags.ContainsKey( id ) )
		{
			Tags.Add( id, new ItemTag( name, color ) );
		}
	}

	public static bool TryGetTag( string id, out ItemTag tag )
	{
		return Tags.TryGetValue( id, out tag );
	}

	public string Name { get; set; }
	public Color Color { get; set; }

	public ItemTag( string name, Color color )
	{
		Name = name;
		Color = color;
	}
}
