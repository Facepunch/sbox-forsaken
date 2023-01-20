
namespace Facepunch.Forsaken;

public interface ILootSpawnerItem
{
	public int LootStackSize { get; }
	public float LootChance { get; }
	public bool IsLootable { get; }
	public string UniqueId { get; }
}
