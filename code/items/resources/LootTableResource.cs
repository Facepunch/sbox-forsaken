using Sandbox;

namespace Facepunch.Forsaken;

public class LootTableResource : ItemResource
{
	[Property]
	public bool IsLootable { get; set; }

	[Property, Range( 0f, 1f ), ShowIf( nameof( IsLootable ), true )]
	public float SpawnChance { get; set; } = 0.5f;

	[Property, ShowIf( nameof( IsLootable ), true )]
	public RangedFloat AmountToSpawn { get; set; }
}
