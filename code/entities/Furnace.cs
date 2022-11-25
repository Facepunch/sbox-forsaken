using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class Furnace : Deployable, IContextActionProvider, ICookerEntity
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	[Net] public CookingProcessor Processor { get; private set; }

	private ContextAction ExtinguishAction { get; set; }
	private ContextAction IgniteAction { get; set; }
	private ContextAction PickupAction { get; set; }
	private ContextAction OpenAction { get; set; }

	private PointLightEntity DynamicLight { get; set; }

	public Furnace()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
		PickupAction.SetCondition( p => Processor.IsEmpty && !Processor.IsActive );

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
		UI.Cooking.Open( player, GetContextName(), this );
	}

	public IEnumerable<ContextAction> GetSecondaryActions()
	{
		yield return OpenAction;
		yield return PickupAction;
	}

	public ContextAction GetPrimaryAction()
	{
		if ( Processor.IsActive )
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
				Processor.Start();
			}
		}
		else if ( action == ExtinguishAction )
		{
			if ( IsServer )
			{
				Processor.Stop();
			}
		}
	}

	public override void Spawn()
	{
		SetModel( "models/furnace/furnace.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Processor = new();
		Processor.SetCooker( this );
		Processor.OnStarted += OnStarted;
		Processor.OnStopped += OnStopped;
		Processor.Fuel.Whitelist.Add( "fuel" );
		Processor.Input.Whitelist.Add( "ore" );

		base.Spawn();
	}

	public override void ClientSpawn()
	{
		Processor.SetCooker( this );
		Processor.OnStarted += OnStarted;
		Processor.OnStopped += OnStopped;

		base.ClientSpawn();
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

		if ( Processor is not null )
		{
			SceneObject?.Attributes?.Set( "Brightness", Processor.IsActive ? 4f : 0f );
		}
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		Processor.Process();
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

	private void OnStarted()
	{
		if ( Host.IsServer ) return;

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

	private void OnStopped()
	{
		if ( Host.IsServer ) return;

		DynamicLight?.Delete();
		DynamicLight = null;
	}
}
