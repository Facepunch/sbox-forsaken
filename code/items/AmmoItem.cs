using Sandbox;
using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

public class AmmoItem : ResourceItem<AmmoResource, AmmoItem>, ILootSpawnerItem
{
	public override Color Color => ItemColors.Ammo;
	public override ushort DefaultStackSize => (ushort)(Resource?.DefaultStackSize ?? 1);
	public override ushort MaxStackSize => (ushort)(Resource?.MaxStackSize ?? 1);
	public virtual AmmoType AmmoType => Resource?.AmmoType ?? AmmoType.None;
	public virtual int AmountToStock => Resource?.AmountToStock.GetValue().CeilToInt() ?? default;
	public virtual float StockChance => Resource?.StockChance ?? default;
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
