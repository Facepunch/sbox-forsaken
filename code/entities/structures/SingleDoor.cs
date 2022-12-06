using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class SingleDoor : Structure, IContextActionProvider
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	private ContextAction OpenAction { get; set; }
	private ContextAction CloseAction { get; set; }

	[Net] public bool IsOpen { get; private set; }

	private Socket Socket { get; set; }

	public SingleDoor()
	{
		CloseAction = new( "close", "Close", "textures/ui/actions/close_door.png" );
		OpenAction = new( "open", "Open", "textures/ui/actions/open_door.png" );

		Tags.Add( "hover" );
	}

	public IEnumerable<ContextAction> GetSecondaryActions()
	{
		yield break;
	}

	public ContextAction GetPrimaryAction()
	{
		if ( IsOpen )
			return CloseAction;
		else
			return OpenAction;
	}

	public string GetContextName()
	{
		return "Door";
	}

	public void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( IsClient ) return;

		if ( action == OpenAction )
			IsOpen = true;
		else if ( action == CloseAction )
			IsOpen = false;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/single_door.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "solid", "door" );
	}

	public override bool CanConnectTo( Socket socket )
	{
		return !FindInSphere( socket.Position, 32f )
			.OfType<Structure>()
			.Where( s => !s.Equals( this ) )
			.Where( s => s.Tags.Has( "door" ) )
			.Any();
	}

	public override void OnNewModel( Model model )
	{
		if ( IsServer || IsClientOnly )
		{
			Socket = AddSocket( "center" );
			Socket.ConnectAny.Add( "doorway" );
			Socket.Tags.Add( "door" );
		}

		base.OnNewModel( model );
	}

	[Event.Tick.Server]
	protected virtual void Tick()
	{
		if ( !Socket.IsValid() ) return;

		var parent = Socket.Connection;
		if ( !parent.IsValid() ) return;

		if ( IsOpen )
			LocalRotation = Rotation.Slerp( LocalRotation, parent.Rotation.RotateAroundAxis( Vector3.Up, 90f ), Time.Delta * 8f );
		else
			LocalRotation = Rotation.Slerp( LocalRotation, parent.Rotation, Time.Delta * 8f );
	}
}
