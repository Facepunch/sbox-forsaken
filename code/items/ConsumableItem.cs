﻿using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class ConsumableItem : ResourceItem<ConsumableResource, ConsumableItem>, ILootTableItem, IConsumableItem
{
	public override Color Color => ItemColors.Consumable;
	public override ushort DefaultStackSize => (ushort)(Resource?.DefaultStackSize ?? 1);
	public override ushort MaxStackSize => (ushort)(Resource?.MaxStackSize ?? 1);
	public virtual int AmountToSpawn => Resource?.AmountToSpawn.GetValue().CeilToInt() ?? default;
	public virtual float SpawnChance => Resource?.SpawnChance ?? default;
	public virtual bool IsLootable => Resource?.IsLootable ?? default;
	public virtual string ConsumeSound => Resource?.ConsumeSound ?? default;
	public virtual string ConsumeEffect => Resource?.ConsumeEffect ?? default;
	public virtual string ActivateSound => Resource?.ActivateSound ?? default;
	public virtual float ActivateDelay => Resource?.ActivateDelay ?? default;
	public virtual List<ConsumableEffect> Effects => Resource?.Effects ?? default;

	public async void Consume( ForsakenPlayer player )
	{
		StackSize--;

		if ( StackSize <= 0 )
			Remove();

		using ( Prediction.Off() )
		{
			if ( !string.IsNullOrEmpty( ConsumeSound ) )
			{
				player.PlaySound( ConsumeSound );
			}
		}

		await GameTask.DelaySeconds( ActivateDelay );

		if ( !player.IsValid() )
			return;

		if ( !string.IsNullOrEmpty( ActivateSound ) )
		{
			player.PlaySound( ActivateSound );
		}

		if ( !string.IsNullOrEmpty( ConsumeEffect ) )
		{
			var effect = Particles.Create( ConsumeEffect, player );
			effect.AutoDestroy( 3f );
			effect.SetEntity( 0, player );
		}

		if ( player.LifeState == LifeState.Alive )
		{
			OnActivated( player );
		}
	}

	public virtual void OnActivated( ForsakenPlayer player )
	{
		foreach ( var effect in Effects )
		{
			Log.Info( effect.Target + " / " + effect.Amount );
		}
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return true;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "consumable" );

		base.BuildTags( tags );
	}
}
