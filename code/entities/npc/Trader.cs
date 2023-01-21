using Editor;
using Sandbox;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Trader" )]
[Model( Model = "models/citizen/citizen.vmdl" )]
public partial class Trader : NPC, IContextActionProvider, IPersistence, INametagProvider
{
	public float InteractionRange => 100f;
	public Color GlowColor => Color.Cyan;
	public bool AlwaysGlow => true;

	[ResourceType( "armor" ), Property] public ArmorResource HeadArmor { get; set; }
	[ResourceType( "armor" ), Property] public ArmorResource ChestArmor { get; set; }
	[ResourceType( "armor" ), Property] public ArmorResource LegsArmor { get; set; }
	[ResourceType( "armor" ), Property] public ArmorResource FeetArmor { get; set; }

	[Net] public TimeUntil NextRestockTime { get; private set; }

	public InventoryContainer Inventory { get; private set; }

	[Property] public float RestockTime { get; set; } = 300f;
	[Property] public float MinSpawnChance { get; set; } = 0f;
	[Property] public float MaxSpawnChance { get; set; } = 1f;
	[Property] public int MaxItemsForSale { get; set; } = 10;

	private ContextAction TalkAction { get; set; }

	public Vector3 EyePosition => Position + Vector3.Up * 72f;
	public Color? NametagColor => Color.Cyan;
	public bool ShowNametag => true;
	public bool IsInactive => false;

	public Trader()
	{
		TalkAction = new( "talk", "Talk", "textures/ui/actions/talk.png" );
	}

	public string GetContextName()
	{
		return DisplayName;
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return TalkAction;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == TalkAction )
		{
			if ( Game.IsServer )
			{

			}
		}
	}

	public virtual bool ShouldSaveState()
	{
		return true;
	}

	public virtual void SerializeState( BinaryWriter writer )
	{
		writer.Write( NextRestockTime.Fraction );
		writer.Write( Inventory );
	}

	public virtual void DeserializeState( BinaryReader reader )
	{
		NextRestockTime = RestockTime * reader.ReadSingle();

		Inventory = reader.ReadInventoryContainer();
		Inventory.SetSlotLimit( (ushort)MaxItemsForSale );
	}

	public virtual void BeforeStateLoaded()
	{

	}

	public virtual void AfterStateLoaded()
	{

	}

	protected virtual void Restock()
	{
		var possibleItems = InventorySystem.GetDefinitions()
			.OfType<IPurchasableItem>()
			.Where( i => i.IsPurchasable )
			.Where( i => i.StockChance > 0f && i.StockChance > MinSpawnChance && i.StockChance < MaxSpawnChance );

		if ( !possibleItems.Any() ) return;

		var itemsToSpawn = MaxItemsForSale;

		for ( var i = 0; i < itemsToSpawn; i++ )
		{
			var u = possibleItems.Sum( p => p.StockChance );
			var r = Game.Random.Float() * u;
			var s = 0f;

			foreach ( var item in possibleItems )
			{
				s += item.StockChance;

				if ( r < s )
				{
					var instance = InventorySystem.CreateItem( item.UniqueId );
					instance.StackSize = (ushort)item.StockStackSize;
					Inventory.Give( instance );
					break;
				}
			}
		}
	}

	[Event.Tick.Server]
	protected virtual void ServerTick()
	{
		if ( NextRestockTime )
		{
			NextRestockTime = RestockTime;
			Restock();
		}
	}

	public override void Spawn()
	{
		var inventory = new InventoryContainer();
		inventory.SetEntity( this );
		inventory.SetSlotLimit( (ushort)MaxItemsForSale );
		InventorySystem.Register( inventory );

		Inventory = inventory;

		AttachArmor( HeadArmor );
		AttachArmor( ChestArmor );
		AttachArmor( LegsArmor );
		AttachArmor( FeetArmor );

		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		NextRestockTime = 0f;

		Tags.Add( "hover", "solid", "trader" );

		base.Spawn();
	}

	private void AttachArmor( ArmorResource resource )
	{
		if ( !string.IsNullOrEmpty( resource.PrimaryModel ) )
		{
			var item = InventorySystem.GetDefinition( resource.UniqueId ) as ArmorItem;

			if ( item.IsValid() )
			{
				AttachArmor( resource.PrimaryModel, item );
			}
		}

		if ( !string.IsNullOrEmpty( resource.SecondaryModel ) )
		{
			var item = InventorySystem.GetDefinition( resource.UniqueId ) as ArmorItem;

			if ( item.IsValid() )
			{
				AttachArmor( resource.SecondaryModel, item );
			}
		}
	}

	private ArmorEntity AttachArmor( string modelName, ArmorItem item )
	{
		var entity = new ArmorEntity();
		entity.SetModel( modelName );
		AttachArmor( entity, item );
		return entity;
	}

	private void AttachArmor( ArmorEntity clothing, ArmorItem item )
	{
		clothing.SetParent( this, true );
		clothing.EnableShadowInFirstPerson = true;
		clothing.EnableHideInFirstPerson = true;
		clothing.Item = item;
	}
}
