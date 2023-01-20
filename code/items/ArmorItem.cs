using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class ArmorItem : ResourceItem<ArmorResource, ArmorItem>, ILootSpawnerItem, IPurchasableItem
{
	public override Color Color => ItemColors.Armor;
	public virtual float TemperatureModifier => Resource?.TemperatureModifier ?? 0f;
	public virtual float DamageMultiplier => Resource?.DamageMultiplier ?? 1f;
	public virtual ArmorSlot ArmorSlot => Resource?.ArmorSlot ?? ArmorSlot.None;
	public virtual string SecondaryModel => Resource?.SecondaryModel ?? string.Empty;
	public virtual string PrimaryModel => Resource?.PrimaryModel ?? string.Empty;
	public virtual int StockStackSize => Resource?.StockStackSize.GetValue().CeilToInt() ?? default;
	public virtual int LootStackSize => Resource?.LootStackSize.GetValue().CeilToInt() ?? default;
	public virtual float StockChance => Resource?.StockChance ?? default;
	public virtual float LootChance => Resource?.LootChance ?? default;
	public virtual int SalvageCost => Resource?.SalvageCost ?? default;
	public virtual bool IsPurchasable => Resource?.IsPurchasable ?? default;
	public virtual bool IsLootable => Resource?.IsLootable ?? default;

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "armor" );

		if ( ArmorSlot == ArmorSlot.Head )
			tags.Add( "head" );
		else if ( ArmorSlot == ArmorSlot.Chest )
			tags.Add( "chest" );
		else if ( ArmorSlot == ArmorSlot.Legs )
			tags.Add( "legs" );

		base.BuildTags( tags );
	}
}
