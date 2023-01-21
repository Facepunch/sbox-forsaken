using Editor;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Scrap Barrel" )]
[Description( "Spawns low tier loot." )]
[EditorModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl_c" )]
public partial class ScrapBarrel : LootSpawner
{
	public override string Title { get; set; } = "Crate";
	public override float RestockTime { get; set; } = 90f;
	public override int SlotLimit { get; set; } = 6;
	public override float MinLootChance { get; set; } = 0.5f;
	public override float MaxLootChance { get; set; } = 1f;

	public override void Spawn()
	{
		SetModel( "models/sbox_props/wooden_crate/wooden_crate.vmdl_c" );

		base.Spawn();
	}
}
