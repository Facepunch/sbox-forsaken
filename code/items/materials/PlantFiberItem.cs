using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class PlantFiberItem : InventoryItem
{
	public override Color Color => ItemColors.Material;
	public override string Name => "Plant Fiber";
	public override string UniqueId => "plant_fiber";
	public override string Description => "Fiber harvested from a plant.";
	public override ushort MaxStackSize => 100;
	public override string Icon => "textures/items/plant_fiber.png";

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "material" );

		base.BuildTags( tags );
	}
}
