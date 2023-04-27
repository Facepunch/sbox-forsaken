using Sandbox;
using System.Collections.Generic;
using System.IO;
using Conna.Inventory;
using Conna.Persistence;

namespace Facepunch.Forsaken;

[ItemEntity]
public partial class ForsakenItemEntity : ItemEntity, IContextActionProvider, IPersistence
{
	public float InteractionRange => 100f;
	public Color GlowColor => Item?.Color ?? Color.White;
	public bool AlwaysGlow => true;

	private ContextAction PickupAction { get; set; }

	public ForsakenItemEntity()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
	}

	public string GetContextName()
	{
		if ( !Item.IsValid() ) return "Unknown Item";

		var item = Item;

		if ( item.StackSize > 1 )
			return $"{item.Name} ({item.StackSize})";
		else
			return item.Name;
	}

	public bool ShouldSaveState()
	{
		return true;
	}

	public void BeforeStateLoaded()
	{

	}

	public void AfterStateLoaded()
	{

	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return PickupAction;
	}

	public void SerializeState( BinaryWriter writer )
	{
		writer.Write( TimeSinceSpawned.Relative );
		writer.Write( Transform );

		if ( Item.IsValid() )
		{
			writer.Write( true );
			writer.Write( Item );
		}
		else
		{
			writer.Write( false );
		}
	}

	public void DeserializeState( BinaryReader reader )
	{
		TimeSinceSpawned = reader.ReadSingle();
		Transform = reader.ReadTransform();

		var isValid = reader.ReadBoolean();
		
		if ( isValid )
		{
			var item = reader.ReadInventoryItem();
			SetItem( item );
		}
		else
		{
			Delete();
		}
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == PickupAction )
		{
			if ( Game.IsServer && IsValid && Item.IsValid() )
			{
				var initialAmount = Item.StackSize;
				var remaining = player.TryGiveItem( Item );

				if ( remaining < initialAmount )
				{
					Sound.FromScreen( To.Single( player ), "inventory.move" );
				}

				if ( remaining == 0 )
				{
					Take();
				}
			}
		}
	}
}

