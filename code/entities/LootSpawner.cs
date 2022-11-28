using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class LootSpawner : ModelEntity, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	[Net] public TimeUntil NextRestockTime { get; private set; }

	public InventoryContainer Inventory { get; private set; }

	public virtual string ModelPath => "models/citizen_props/crate01.vmdl";
	public virtual string Title => "Loot Spawner";
	public virtual float RestockTime => 30f;
	public virtual int SlotLimit => 6;

	private ContextAction OpenAction { get; set; }
	private bool IsHidden { get; set; }

	public LootSpawner()
	{
		OpenAction = new( "open", "Open", "textures/ui/actions/open.png" );
	}

	public string GetContextName()
	{
		return Title;
	}

	public void Open( ForsakenPlayer player )
	{
		UI.Storage.Open( player, GetContextName(), this, Inventory );
	}

	public IEnumerable<ContextAction> GetSecondaryActions()
	{
		yield break;
	}

	public ContextAction GetPrimaryAction()
	{
		return OpenAction;
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
	}

	public override void Spawn()
	{
		SetModel( "models/military_crate/military_crate.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		var inventory = new InventoryContainer();
		inventory.IsTakeOnly = true;
		inventory.SetEntity( this );
		inventory.SetSlotLimit( (ushort)SlotLimit );
		inventory.SlotChanged += OnSlotChanged;
		InventorySystem.Register( inventory );

		Inventory = inventory;

		NextRestockTime = 0f;
		Hide();

		base.Spawn();
	}

	protected virtual void Restock()
	{

	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		if ( NextRestockTime && IsHidden )
		{
			if ( !IsAreaClear() )
			{
				NextRestockTime = RestockTime;
				return;
			}

			Restock();
			Show();
		}
	}

	private void OnSlotChanged( ushort slot )
	{
		if ( Inventory.IsEmpty )
		{
			NextRestockTime = RestockTime;
			Hide();
		}
	}

	private bool IsAreaClear()
	{
		var entities = FindInSphere( Position, 32f ).Where( e => !e.Equals( this ) );
		return !entities.Any();
	}

	private void Hide()
	{
		EnableAllCollisions = false;
		EnableDrawing = false;
		IsHidden = true;
	}

	private void Show()
	{
		EnableAllCollisions = true;
		EnableDrawing = true;
		IsHidden = false;
	}
}
