using Sandbox;
using Editor;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Loot Spawner" )]
[Model]
public partial class GenericLootSpawner : LootSpawner
{
	[Property] public override string Title { get; set; } = "Container";
	[Property] public override float RestockTime { get; set; } = 30f;
	[Property] public override int SlotLimit { get; set; } = 6;
	[Property] public override float MinSpawnChance { get; set; } = 0f;
	[Property] public override float MaxSpawnChance { get; set; } = 1f;
}
