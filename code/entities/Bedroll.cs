using Sandbox;
using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

public partial class Bedroll : Deployable, IContextActionProvider, IHeatEmitter, IPersistence
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	private ContextAction MakeHomeAction { get; set; }
	private ContextAction PickupAction { get; set; }

	public float EmissionRadius => 50f;
	public float HeatToEmit => 5f;

	public PersistenceHandle Handle { get; private set; }

	[Net] private long OwnerId { get; set; }

	public Bedroll()
	{
		PickupAction = new( "pickup", "Pickup", "textures/ui/actions/pickup.png" );
		PickupAction.SetCondition( p => p.Client.SteamId == OwnerId );

		MakeHomeAction = new( "home", "Make Home", "textures/ui/actions/make_home.png" );
		MakeHomeAction.SetCondition( p => p.Client.SteamId == OwnerId && p.Bedroll != this );
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

	public void SerializeState( BinaryWriter writer )
	{
		writer.Write( Handle );
		writer.Write( Transform );
		writer.Write( OwnerId );
	}

	public void DeserializeState( BinaryReader reader )
	{
		Handle = reader.ReadPersistenceHandle();
		Transform = reader.ReadTransform();
		OwnerId = reader.ReadInt64();
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
				var item = InventorySystem.CreateItem<BedrollItem>();
				player.TryGiveItem( item );
				player.PlaySound( "inventory.move" );
				Delete();
			}
		}
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

		Handle = new();

		base.Spawn();
	}
}
