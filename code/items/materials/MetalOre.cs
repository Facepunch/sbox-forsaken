namespace Facepunch.Forsaken;

public class MetalOre : InventoryItem
{
	public override Color Color => ItemColors.Material;
	public override string Name => "Metal Ore";
	public override string UniqueId => "metal_ore";
	public override string Description => "Raw metal ore as extracted directly from a deposit.";
	public override ushort MaxStackSize => 20;
	public override string Icon => "textures/items/metal_ore.png";
}
