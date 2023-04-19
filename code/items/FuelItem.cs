using Sandbox;
using System.Collections.Generic;
using Conna.Inventory;

namespace Facepunch.Forsaken;

public class FuelItem : InventoryItem, ILootSpawnerItem
{
	public override Color Color => ItemColors.Material;
	public override string Description => "A highly combustible liquid.";
	public override ushort MaxStackSize => 500;
	public override string UniqueId => "fuel";
	public override string Name => "Fuel";
	public override string Icon => "textures/items/fuel.png";

	public bool OncePerContainer => true;
	public int LootStackSize => Game.Random.Int( 5, 20 );
	public float LootChance => 0.5f;
	public bool IsLootable => true;

	public override bool CanStackWith( InventoryItem other )
	{
		return true;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "fuel" );

		base.BuildTags( tags );
	}
}
