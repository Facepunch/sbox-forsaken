namespace Facepunch.Forsaken;

public struct ItemTag
{
	public static ItemTag Construction { get; private set; } = new ItemTag( "Construction", ItemColors.Tool );
	public static ItemTag Consumable { get; private set; } = new ItemTag( "Consumable", ItemColors.Consumable );
	public static ItemTag Deployable { get; private set; } = new ItemTag( "Deployable", ItemColors.Deployable );

	public string Name { get; set; }
	public Color Color { get; set; }

	public ItemTag( string name, Color color )
	{
		Name = name;
		Color = color;
	}
}
