using Sandbox;
using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

public class AmmoItem : ResourceItem<AmmoResource, AmmoItem>, ILootSpawnerItem, IPurchasableItem
{
	public override Color Color => ItemColors.Ammo;
	public override ushort DefaultStackSize => (ushort)(Resource?.DefaultStackSize ?? 1);
	public override ushort MaxStackSize => (ushort)(Resource?.MaxStackSize ?? 1);
	public virtual AmmoType AmmoType => Resource?.AmmoType ?? AmmoType.None;
	public virtual int StockStackSize => Resource?.StockStackSize.GetValue().CeilToInt() ?? default;
	public virtual int LootStackSize => Resource?.LootStackSize.GetValue().CeilToInt() ?? default;
	public virtual float StockChance => Resource?.StockChance ?? default;
	public virtual float LootChance => Resource?.LootChance ?? default;
	public virtual int SalvageCost => Resource?.SalvageCost ?? default;
	public virtual bool IsPurchasable => Resource?.IsPurchasable ?? default;
	public virtual bool IsLootable => Resource?.IsLootable ?? default;

	public override bool CanStackWith( InventoryItem other )
	{
		return (other is AmmoItem item && item.AmmoType == AmmoType);
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "ammo" );

		base.BuildTags( tags );
	}
}
