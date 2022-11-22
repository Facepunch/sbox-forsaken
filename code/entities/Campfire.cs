using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class Campfire : Deployable, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	[Net] private NetInventoryContainer InternalInventory { get; set; }
	public InventoryContainer Inventory => InternalInventory.Value;

	private ContextAction ExtinguishAction { get; set; }
	private ContextAction IgniteAction { get; set; }
	private ContextAction PickupAction { get; set; }
	private ContextAction OpenAction { get; set; }

	[Net, Change( nameof( OnIsBurningChanged ) )] public bool IsBurning { get; private set; }
	[Net] public bool IsEmpty { get; private set; }

	private Particles ParticleEffect { get; set; }

	public Campfire()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
		PickupAction.SetCondition( p => IsEmpty && !IsBurning );

		OpenAction = new( "open", "Open", "textures/ui/actions/open.png" );

		IgniteAction = new( "ignore", "Ignite", "textures/ui/actions/ignite.png" );
		ExtinguishAction = new( "extinguish", "Extinguish", "textures/ui/actions/disable.png" );
	}

	public string GetContextName()
	{
		return "Campfire";
	}

	public void Open( ForsakenPlayer player )
	{
		UI.Storage.Open( player, GetContextName(), this, Inventory );
	}

	public IEnumerable<ContextAction> GetSecondaryActions()
	{
		yield return OpenAction;
		yield return PickupAction;
	}

	public ContextAction GetPrimaryAction()
	{
		if ( IsBurning )
			return ExtinguishAction;
		else
			return IgniteAction;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == OpenAction )
		{
			if ( IsServer )
			{
				Open( player );
			}
		}
		else if ( action == PickupAction )
		{
			if ( IsServer )
			{
				var item = InventorySystem.CreateItem<CampfireItem>();
				player.TryGiveItem( item );
				player.PlaySound( "inventory.move" );
				Delete();
			}
		}
		else if ( action == IgniteAction )
		{
			if ( IsServer )
			{
				IsBurning = true;
			}
		}
		else if ( action == ExtinguishAction )
		{
			if ( IsServer )
			{
				IsBurning = false;
			}
		}
	}

	public override void Spawn()
	{
		SetModel( "models/campfire/campfire.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		var inventory = new InventoryContainer( this );
		inventory.SetSlotLimit( 1 );
		inventory.OnSlotChanged += OnSlotChanged;
		InventorySystem.Register( inventory );

		InternalInventory = new NetInventoryContainer( inventory );
		IsEmpty = inventory.IsEmpty;

		base.Spawn();
	}

	private void OnIsBurningChanged()
	{
		ParticleEffect?.Destroy();
		ParticleEffect = null;

		if ( IsBurning )
		{
			ParticleEffect = Particles.Create( "particles/campfire/campfire.vpcf", this );
		}
	}

	private void OnSlotChanged( ushort slot )
	{
		if ( IsServer )
		{
			IsEmpty = Inventory.IsEmpty;
		}
	}
}
