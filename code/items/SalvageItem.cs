using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class SalvageItem : InventoryItem
{
	public override Color Color => ItemColors.Material;
	public override string Description => "The remnants of something useful. It could be repurposed into something else.";
	public override ushort MaxStackSize => 1000;
	public override string UniqueId => "salvage";
	public override string Name => "Salvage";
	public override string Icon => "textures/items/salvage.png";

	public override bool CanStackWith( InventoryItem other )
	{
		return true;
	}
}
