using Editor;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Military Crate" )]
[EditorModel( "models/military_crate/military_crate.vmdl" )]
public partial class MilitaryCrate : LootSpawner
{
	public override string Title { get; set; } = "Military Crate";
	public override float RestockTime { get; set; } = 30f;
	public override int SlotLimit { get; set; } = 6;
	public override float MinSpawnChance { get; set; } = 0f;
	public override float MaxSpawnChance { get; set; } = 0.5f;

	public override void Spawn()
	{
		SetModel( "models/military_crate/military_crate.vmdl" );

		base.Spawn();
	}
}
