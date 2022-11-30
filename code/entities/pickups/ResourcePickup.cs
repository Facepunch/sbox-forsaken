using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public abstract partial class ResourcePickup : ModelEntity, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	private ContextAction HarvestAction { get; set; }

	public ResourcePickup()
	{
		HarvestAction = new( "harvest", "Harvest", "textures/ui/actions/pickup.png" );
	}

	public IEnumerable<ContextAction> GetSecondaryActions()
	{
		yield break;
	}

	public ContextAction GetPrimaryAction()
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
			if ( IsServer )
			{
				player.StartTimedAction( "Harvesting...", Position, 3f, () =>
				{
					if ( IsValid )
					{
						var item = InventorySystem.CreateItem<WoodItem>();
						item.StackSize = 20;

						player.TryGiveItem( item );
						player.PlaySound( "inventory.move" );

						Delete();
					}
				} );
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
		SetModel( "models/resources/tree_stump.vmdl" );

		Tags.Add( "pickup" );

		base.Spawn();
	}
}
