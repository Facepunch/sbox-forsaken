
namespace Facepunch.Forsaken;

public interface IPurchasableItem
{
	public int AmountToStock { get; }
	public float StockChance { get; }
	public bool IsPurchasable { get; }
	public int SalvageCost { get; }
	public string UniqueId { get; }
}
