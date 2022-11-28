using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class ArmorItem : ResourceItem<ArmorResource, ArmorItem>, ILootTableItem
{
	public override Color Color => ItemColors.Armor;
	public virtual float TemperatureModifier => Resource?.TemperatureModifier ?? 0f;
	public virtual float DamageMultiplier => Resource?.DamageMultiplier ?? 1f;
	public virtual ArmorSlot ArmorSlot => Resource?.ArmorSlot ?? ArmorSlot.None;
	public virtual string SecondaryModel => Resource?.SecondaryModel ?? string.Empty;
	public virtual string PrimaryModel => Resource?.PrimaryModel ?? string.Empty;
	public virtual float SpawnChance => Resource?.SpawnChance ?? default;
	public virtual int AmountToSpawn => Resource?.AmountToSpawn.GetValue().CeilToInt() ?? default;
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
