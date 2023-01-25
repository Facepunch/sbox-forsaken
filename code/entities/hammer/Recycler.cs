using Editor;
using Sandbox;
using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Recycler" )]
[Description( "Allows players to recycle various items." )]
[EditorModel( "models/citizen_props/recyclingbin01.vmdl" )]
public partial class Recycler : ModelEntity, IContextActionProvider, IPersistence
{
	public float InteractionRange => 100f;
	public bool AlwaysGlow => true;
	public Color GlowColor => Color.Green;

	[ConCmd.Server( "fsk.recycler.toggle" )]
	public static void ToggleCmd( int entityId )
	{
		var recycler = FindByIndex( entityId ) as Recycler;

		if ( recycler.IsValid() )
		{
			if ( recycler.Processor.IsActive )
				recycler.Processor.Stop();
			else
				recycler.Processor.Start();
		}
	}

	[Net] public RecyclingProcessor Processor { get; private set; }


	private ContextAction OpenAction { get; set; }
	private Sound? ActiveSound { get; set; }

	public Recycler()
	{
		OpenAction = new( "open", "Open", "textures/ui/actions/open.png" );
	}

	public string GetContextName()
	{
		return "Recycler";
	}

	public void Open( ForsakenPlayer player )
	{
		UI.Recycling.Open( player, GetContextName(), this );
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		yield break;
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		return OpenAction;
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
		Processor.SerializeState( writer );
	}

	public void DeserializeState( BinaryReader reader )
	{
		Processor.DeserializeState( reader );
	}

	public void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( action == OpenAction )
		{
			if ( Game.IsServer )
			{
				Open( player );
			}
		}
	}

	public override void Spawn()
	{
		SetModel( "models/citizen_props/recyclingbin01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Processor = new();
		Processor.SetRecycler( this );

		Tags.Add( "hover", "solid" );

		base.Spawn();
	}

	public override void ClientSpawn()
	{
		Processor.SetRecycler( this );

		base.ClientSpawn();
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		if ( Processor.IsActive )
		{
			if ( !ActiveSound.HasValue )
				ActiveSound = PlaySound( "recycler.loop" );
		}
		else
		{
			ActiveSound?.Stop();
			ActiveSound = null;
		}

		Processor.Process();
	}
}
