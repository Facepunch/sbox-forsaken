using System.Collections.Generic;
using Conna.Inventory;

namespace Facepunch.Forsaken;

public class MetalOre : InventoryItem, ICookableItem
{
	public override Color Color => ItemColors.Material;
	public override string Name => "Metal Ore";
	public override string UniqueId => "metal_ore";
	public override string Description => "Raw metal ore as extracted directly from a deposit.";
	public override ushort MaxStackSize => 500;
	public override string Icon => "textures/items/metal_ore.png";

	public string CookedItemId => "metal_fragments";
	public int CookedQuantity => 1;

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "ore" );
		tags.Add( "material" );

		base.BuildTags( tags );
	}
}
