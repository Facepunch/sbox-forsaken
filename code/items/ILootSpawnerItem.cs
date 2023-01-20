
namespace Facepunch.Forsaken;

public interface ILootSpawnerItem
{
	public int AmountToStock { get; }
	public float StockChance { get; }
	public bool IsLootable { get; }
	public string UniqueId { get; }
}
