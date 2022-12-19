using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class LootSpawner : ModelEntity, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.Green;
	public float GlowWidth => 0.4f;

	[Net] public TimeUntil NextRestockTime { get; private set; }

	public InventoryContainer Inventory { get; private set; }

	public virtual string Title { get; set; } = "Loot Spawner";
	public virtual float RestockTime { get; set; } = 30f;
	public virtual int SlotLimit { get; set; } = 6;
	public virtual float MinSpawnChance { get; set; } = 0f;
	public virtual float MaxSpawnChance { get; set; } = 1f;

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

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return OpenAction;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == OpenAction )
		{
			if ( Game.IsServer )
			{
				Open( player );
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
		var inventory = new InventoryContainer();
		inventory.IsTakeOnly = true;
		inventory.SetEntity( this );
		inventory.SetSlotLimit( (ushort)SlotLimit );
		inventory.SlotChanged += OnSlotChanged;
		InventorySystem.Register( inventory );

		Inventory = inventory;

		NextRestockTime = 0f;
		Hide();

		Tags.Add( "hover", "solid" );

		base.Spawn();
	}

	protected virtual void Restock()
	{
		var possibleItems = InventorySystem.GetDefinitions()
			.OfType<ILootTableItem>()
			.Where( i => i.IsLootable )
			.Where( i => i.SpawnChance > 0f && i.SpawnChance > MinSpawnChance && i.SpawnChance < MaxSpawnChance );

		if ( !possibleItems.Any() ) return;

		var itemsToSpawn = Game.Random.Int( 1, SlotLimit );

		for ( var i = 0; i < itemsToSpawn; i++ )
		{
			var u = possibleItems.Sum( p => p.SpawnChance );
			var r = Game.Random.Float() * u;
			var s = 0f;

			foreach ( var item in possibleItems )
			{
				s += item.SpawnChance;

				if ( r < s )
				{
					var instance = InventorySystem.CreateItem( item.UniqueId );
					instance.StackSize = (ushort)item.AmountToSpawn;
					Inventory.Stack( instance );
					break;
				}
			}
		}
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
