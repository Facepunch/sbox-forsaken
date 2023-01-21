using Editor;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Basic Loot Crate" )]
[Description( "Spawns low to medium tier loot." )]
[EditorModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl_c" )]
public partial class BasicLootCrate : LootSpawner
{
	public override string Title { get; set; } = "Crate";
	public override float RestockTime { get; set; } = 180f;
	public override int SlotLimit { get; set; } = 4;
	public override float MinLootChance { get; set; } = 0.2f;
	public override float MaxLootChance { get; set; } = 1f;

	public override void Spawn()
	{
		SetModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl_c" );

		base.Spawn();
	}
}
