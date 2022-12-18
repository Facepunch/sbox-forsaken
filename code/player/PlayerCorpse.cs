using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class PlayerCorpse : ModelEntity, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	private InventoryContainer Inventory { get; set; }
	private ContextAction SearchAction { get; set; }
	private TimeSince TimeSinceSpawned { get; set; }
	
	[Net] private string PlayerName { get; set; }

	public PlayerCorpse()
	{
		UsePhysicsCollision = true;
		TimeSinceSpawned = 0f;
		PhysicsEnabled = true;
		SearchAction = new( "search", "Search", "textures/ui/actions/open.png" );
	}

	public string GetContextName()
	{
		return $"{PlayerName}'s Corpse";
	}

	public void Search( ForsakenPlayer player )
	{
		UI.Storage.Open( player, GetContextName(), this, Inventory );
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return SearchAction;
	}

	public void CopyFrom( ForsakenPlayer player )
	{
		RenderColor = player.RenderColor;

		SetModel( player.GetModelName() );
		TakeDecalsFrom( player );

		this.CopyBonesFrom( player );
		this.SetRagdollVelocityFrom( player );

		foreach ( var child in player.Children )
		{
			if ( child is ArmorEntity e )
			{
				var model = e.GetModelName();
				var armor = new ArmorEntity();

				armor.RenderColor = e.RenderColor;
				armor.SetModel( model );
				armor.SetParent( this, true );
				armor.Item = e.Item;
			}
		}

		var items = player.FindItems<InventoryItem>();

		var inventory = new InventoryContainer();
		inventory.SetEntity( this );
		inventory.ItemTaken += OnItemTaken;
		inventory.SetSlotLimit( (ushort)items.Count() );
		inventory.IsTakeOnly = true;
		InventorySystem.Register( inventory );

		foreach ( var item in items )
		{
			inventory.Give( item );
		}

		PlayerName = player.Client.Name;
		Inventory = inventory;
	}

	public void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == SearchAction )
		{
			if ( Game.IsServer )
			{
				Search( player );
			}
		}
	}

	public void ApplyForceToBone( Vector3 force, int forceBone )
	{
		PhysicsGroup.AddVelocity( force );

		if ( forceBone >= 0 )
		{
			var body = GetBonePhysicsBody( forceBone );

			if ( body != null )
				body.ApplyForce( force * 1000 );
			else
				PhysicsGroup.AddVelocity( force );
		}
	}

	public override void Spawn()
	{
		Tags.Add( "hover", "passplayers" );

		base.Spawn();
	}

	[Event.Tick.Client]
	private void ClientTick()
	{
		if ( IsClientOnly && TimeSinceSpawned > 120f )
		{
			Delete();
		}
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		if ( TimeSinceSpawned > 600f )
		{
			Delete();
		}
	}

	private void OnItemTaken( ushort slot, InventoryItem instance )
	{
		var armor = Children.OfType<ArmorEntity>().Where( c => c.Item == instance ).ToList();

		foreach ( var entity in armor )
		{
			if ( entity.IsValid() )
			{
				entity.Delete();
			}
		}

		if ( Inventory.IsEmpty )
		{
			Delete();
		}
	}
}
