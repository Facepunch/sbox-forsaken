using Sandbox;
using System.Collections.Generic;
using System.IO;
using Conna.Inventory;

namespace Facepunch.Forsaken;

public partial class Bedroll : Deployable, IContextActionProvider, IHeatEmitter
{
	public float InteractionRange => 100f;
	public bool AlwaysGlow => false;
	public Color GlowColor => Color.White;

	private ContextAction MakeHomeAction { get; set; }
	private ContextAction PickupAction { get; set; }

	public float EmissionRadius => 50f;
	public float HeatToEmit => 5f;

	[Net] private long OwnerId { get; set; }

	public Bedroll()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
		PickupAction.SetCondition( p =>
		{
			return new ContextAction.Availability
			{
				IsAvailable = p.Client.SteamId == OwnerId
			};
		} );

		MakeHomeAction = new( "home", "Make Home", "textures/ui/actions/make_home.png" );
		MakeHomeAction.SetCondition( p =>
		{
			return new ContextAction.Availability
			{
				IsAvailable = p.Client.SteamId == OwnerId && p.Bedroll != this
			};
		} );
	}

	public string GetContextName()
	{
		return "Bedroll";
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield return PickupAction;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return MakeHomeAction;
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == MakeHomeAction )
		{
			if ( Game.IsServer )
			{
				player.SetBedroll( this );
			}
		}
		else if ( action == PickupAction )
		{
			if ( Game.IsServer )
			{
				Sound.FromScreen( To.Single( player ), "inventory.move" );

				var item = InventorySystem.CreateItem<BedrollItem>();
				player.TryGiveItem( item );
				Delete();
			}
		}
	}

	public override void SerializeState( BinaryWriter writer )
	{
		base.SerializeState( writer );

		writer.Write( OwnerId );
	}

	public override void DeserializeState( BinaryReader reader )
	{
		base.DeserializeState( reader );

		OwnerId = reader.ReadInt64();
	}

	public override void OnPlacedByPlayer( ForsakenPlayer player, TraceResult trace )
	{
		OwnerId = player.Client.SteamId;
		base.OnPlacedByPlayer( player, trace );
	}

	public override void Spawn()
	{
		SetModel( "models/bedroll/bedroll.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		SphereTrigger.Attach( this, EmissionRadius );

		Tags.Add( "hover", "solid", "passplayers" );

		base.Spawn();
	}
}
