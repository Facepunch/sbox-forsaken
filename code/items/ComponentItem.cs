using Sandbox;
using System.Collections.Generic;
using System.IO;
using Conna.Inventory;

namespace Facepunch.Forsaken;

public class ComponentItem : ResourceItem<ComponentResource, ComponentItem>, ILootSpawnerItem, IPurchasableItem, IRecyclableItem
{
	public override Color Color => ItemColors.Component;
	public override ushort DefaultStackSize => (ushort)(Resource?.DefaultStackSize ?? 1);
	public override ushort MaxStackSize => (ushort)(Resource?.MaxStackSize ?? 1);
	public virtual int StockStackSize => Resource?.StockStackSize.GetValue().CeilToInt() ?? default;
	public virtual int LootStackSize => Resource?.LootStackSize.GetValue().CeilToInt() ?? default;
	public virtual bool OncePerContainer => Resource?.OncePerContainer ?? default;
	public virtual float StockChance => Resource?.StockChance ?? default;
	public virtual float LootChance => Resource?.LootChance ?? default;
	public virtual int SalvageCost => Resource?.SalvageCost ?? default;
	public virtual bool IsPurchasable => Resource?.IsPurchasable ?? default;
	public virtual bool IsLootable => Resource?.IsLootable ?? default;
	public virtual Dictionary<string, int> RecycleOutput => Resource?.RecycleOutput ?? default;
	public virtual float BaseComponentReturn => Resource?.BaseComponentReturn ?? 0.5f;
	public virtual bool IsRecyclable => Resource?.IsRecyclable ?? default;

	public override bool CanStackWith( InventoryItem other )
	{
		return true;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "component" );

		base.BuildTags( tags );
	}
}
