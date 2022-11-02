namespace Facepunch.Forsaken;

public struct ItemTag
{
	public static ItemTag Consumable { get; private set; } = new ItemTag( "Consumable", Color.Green );

	public string Name { get; set; }
	public Color Color { get; set; }

	public ItemTag( string name, Color color )
	{
		Name = name;
		Color = color;
	}
}
