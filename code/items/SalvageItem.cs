using Sandbox;
using System.Collections.Generic;
using Conna.Inventory;

namespace Facepunch.Forsaken;

public class SalvageItem : InventoryItem, ILootSpawnerItem
{
	public override Color Color => ItemColors.Material;
	public override string Description => "The remnants of something useful. It could be repurposed into something else.";
	public override ushort MaxStackSize => 1000;
	public override string UniqueId => "salvage";
	public override string Name => "Salvage";
	public override string Icon => "textures/items/salvage.png";

	public bool OncePerContainer => false;
	public int LootStackSize => Game.Random.Int( 2, 4 );
	public float LootChance => 0.8f;
	public bool IsLootable => true;

	public override bool CanStackWith( IInventoryItem other )
	{
		return true;
	}
}
