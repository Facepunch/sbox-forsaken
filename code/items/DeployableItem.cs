using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class DeployableItem : InventoryItem, ILootSpawnerItem, IPurchasableItem
{
	public override Color Color => ItemColors.Deployable;
	public override string PrimaryUseHint => "Deploy";

	public virtual Type Deployable => null;
	public virtual string PlaceSoundName => "deployable.place";
	public virtual string Model => "models/citizen_props/crate01.vmdl";
	public virtual string[] ValidTags => new string[] { "world" };
	public virtual bool IsStructure => false;

	public virtual int StockStackSize => 1;
	public virtual int LootStackSize => 1;
	public virtual float StockChance => 0.5f;
	public virtual float LootChance => 0.5f;
	public virtual int SalvageCost => 1;
	public virtual bool IsPurchasable => false;
	public virtual bool IsLootable => false;
	public virtual bool OncePerContainer => true;

	public virtual bool CanPlaceOn( Entity entity )
	{
		return false;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "deployable" );

		base.BuildTags( tags );
	}
}
