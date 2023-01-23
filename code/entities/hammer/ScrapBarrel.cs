using Editor;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Scrap Barrel" )]
[Description( "Spawns low tier loot." )]
[EditorModel( "models/rust_props/barrels/fuel_barrel.vmdl" )]
public partial class ScrapBarrel : LootSpawner
{
	public override string BreakSound { get; set; } = "fsk.break.barrel";
	public override string Title { get; set; } = "Barrel";
	public override float RestockTime { get; set; } = 90f;
	public override int SlotLimit { get; set; } = 2;
	public override float MinLootChance { get; set; } = 0.6f;
	public override float MaxLootChance { get; set; } = 1f;

	public override void Spawn()
	{
		SetModel( "models/rust_props/barrels/fuel_barrel.vmdl" );

		base.Spawn();
	}
}
