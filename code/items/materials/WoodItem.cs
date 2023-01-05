using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class WoodItem : InventoryItem
{
	public override Color Color => ItemColors.Material;
	public override string Name => "Wood";
	public override string UniqueId => "wood";
	public override string Description => "Wood from a tree. Usually obtained by beating one with an axe.";
	public override ushort MaxStackSize => 100;
	public override string Icon => "textures/items/wood.png";

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "fuel" );
		tags.Add( "material" );

		base.BuildTags( tags );
	}
}
