using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class ForsakenItemResource : ItemResource
{
	private bool IsLootableOrPurchasable => IsLootable || IsPurchasable;

	/// <summary>
	/// Can this item be recycled?
	/// </summary>
	[Property]
	public bool IsRecyclable { get; set; }

	/// <summary>
	/// The unique item ids and their quantities to output as a result of recycling this item.
	/// </summary>
	[Property, ShowIf( nameof( IsRecyclable ), true )] public Dictionary<string, int> RecycleOutput { get; set; }

	/// <summary>
	/// Is this item lootable from a loot spawner?
	/// </summary>
	[Property]
	public bool IsLootable { get; set; }

	/// <summary>
	/// Can this item be purchased from a trader?
	/// </summary>
	[Property]
	public bool IsPurchasable { get; set; }

	/// <summary>
	/// How much Salvage does it cost to purchase this item from a trader?
	/// </summary>
	[Property, ShowIf( nameof( IsPurchasable ), true )]
	public int SalvageCost { get; set; } = 0;

	/// <summary>
	/// What is the chance that this item will be stocked for a trader or loot spawner?
	/// </summary>
	[Property, Range( 0f, 1f ), ShowIf( nameof( IsLootableOrPurchasable ), true )]
	public float StockChance { get; set; } = 0.5f;

	/// <summary>
	/// How many of this item can be stocked in a single stack?
	/// </summary>
	[Property, ShowIf( nameof( IsLootableOrPurchasable ), true )]
	public RangedFloat AmountToStock { get; set; }
}
