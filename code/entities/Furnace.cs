using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class Furnace : Deployable, IContextActionProvider
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

	private PointLightEntity DynamicLight { get; set; }

	public Furnace()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
		PickupAction.SetCondition( p => IsEmpty && !IsBurning );

		OpenAction = new( "open", "Open", "textures/ui/actions/open.png" );

		IgniteAction = new( "ignore", "Ignite", "textures/ui/actions/ignite.png" );
		ExtinguishAction = new( "extinguish", "Extinguish", "textures/ui/actions/disable.png" );
	}

	public string GetContextName()
	{
		return "Furnace";
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
		SetModel( "models/furnace/furnace.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		var inventory = new InventoryContainer();
		inventory.SetEntity( this );
		inventory.SetSlotLimit( 1 );
		inventory.SlotChanged += OnSlotChanged;
		InventorySystem.Register( inventory );

		InternalInventory = new NetInventoryContainer( inventory );
		IsEmpty = inventory.IsEmpty;

		base.Spawn();
	}

	protected override void OnDestroy()
	{
		DynamicLight?.Delete();
		DynamicLight = null;

		base.OnDestroy();
	}

	[Event.Tick.Client]
	private void ClientTick()
	{
		if ( DynamicLight.IsValid() )
		{
			UpdateDynamicLight();
		}

		SceneObject?.Attributes?.Set( "Brightness", IsBurning ? 4f : 0f );
	}

	private void UpdateDynamicLight()
	{
		var position = Position;
		var attachment = GetAttachment( "fire" );

		if ( attachment.HasValue )
			position = attachment.Value.Position;

		DynamicLight.Brightness = 0.1f + MathF.Sin( Time.Now * 4f ) * 0.02f;
		DynamicLight.Position = position;
		DynamicLight.Range = 200f + MathF.Sin( Time.Now ) * 50f;
	}

	private void OnIsBurningChanged()
	{
		if ( IsBurning )
		{
			if ( !DynamicLight.IsValid() )
			{
				DynamicLight = new();
				DynamicLight.SetParent( this );
				DynamicLight.EnableShadowCasting = true;
				DynamicLight.DynamicShadows = true;
				DynamicLight.Color = Color.Orange;

				UpdateDynamicLight();
			}
		}
		else
		{
			DynamicLight?.Delete();
			DynamicLight = null;
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
