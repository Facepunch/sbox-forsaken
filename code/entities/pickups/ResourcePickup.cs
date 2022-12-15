using System;
using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public abstract partial class ResourcePickup : ModelEntity, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	private ContextAction HarvestAction { get; set; }

	public abstract string ModelPath { get; }
	public abstract Type ItemType { get; }
	public abstract int StackSize { get; }

	public ResourcePickup()
	{
		HarvestAction = new( "harvest", "Harvest", "textures/ui/actions/harvest.png" );
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return HarvestAction;
	}

	public virtual string GetContextName()
	{
		return "Resource";
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == HarvestAction )
		{
			if ( Game.IsServer )
			{
				var timedAction = new TimedActionInfo( OnHarvested );

				timedAction.Title = "Harvesting";
				timedAction.Origin = Position;
				timedAction.Duration = 2f;
				timedAction.Icon = "textures/ui/actions/pickup.png";

				player.StartTimedAction( timedAction );
			}
		}
	}

	public override void OnNewModel( Model model )
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		base.OnNewModel( model );
	}

	public override void Spawn()
	{
		SetModel( ModelPath );

		Tags.Add( "hover", "solid", "passplayers" );

		base.Spawn();
	}

	private void OnHarvested( ForsakenPlayer player )
	{
		if ( IsValid )
		{
			var item = InventorySystem.CreateItem( ItemType );
			item.StackSize = (ushort)StackSize;

			var remaining = player.TryGiveItem( item );

			if ( remaining < StackSize )
			{
				player.PlaySound( "inventory.move" );
			}

			if ( remaining == StackSize ) return;

			if ( remaining > 0 )
			{
				var entity = new ItemEntity();
				entity.Position = Position;
				entity.SetItem( item );
			}

			Delete();
		}
	}
}
