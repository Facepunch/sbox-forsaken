using Sandbox;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class SingleDoor : Structure, IContextActionProvider, ICodeLockable
{
	public float InteractionRange => 150f;
	public Color GlowColor => IsAuthorized() ? Color.Green : Color.Red;
	public float GlowWidth => 0.4f;

	private ContextAction OpenAction { get; set; }
	private ContextAction CloseAction { get; set; }
	private ContextAction LockAction { get; set; }
	private ContextAction AuthorizeAction { get; set; }

	[Net] private IList<long> Authorized { get; set; } = new List<long>();
	[Net] public bool IsLocked { get; private set; }
	[Net] public bool IsOpen { get; private set; }

	private Socket Socket { get; set; }

	public string Code { get; private set; }

	public SingleDoor()
	{
		CloseAction = new( "close", "Close", "textures/ui/actions/close_door.png" );
		CloseAction.SetCondition( IsAuthorized );

		OpenAction = new( "open", "Open", "textures/ui/actions/open_door.png" );
		OpenAction.SetCondition( IsAuthorized );

		LockAction = new( "lock", "Lock", "textures/items/code_lock.png" );
		LockAction.SetCondition( CanBeLockedBy );

		AuthorizeAction = new( "authorize", "Authorize", "textures/ui/actions/authorize.png" );
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		if ( !IsLocked )
		{
			yield return LockAction;
		}
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		if ( IsLocked && !IsAuthorized( player ) )
			return AuthorizeAction;

		if ( IsOpen )
			return CloseAction;
		else
			return OpenAction;
	}

	public bool ApplyLock( ForsakenPlayer player, string code )
	{
		if ( !player.HasItems<CodeLockItem>( 1 ) )
			return false;

		IsLocked = true;
		Code = code;

		player.TakeItems<CodeLockItem>( 1 );

		return true;
	}

	public void Authorize( ForsakenPlayer player )
	{
		if ( IsAuthorized( player ) ) return;
		Authorized.Add( player.SteamId );
	}

	public bool CanBeLockedBy( ForsakenPlayer player )
	{
		return IsAuthorized( player ) && player.HasItems<CodeLockItem>( 1 );
	}

	public void Deauthorize( ForsakenPlayer player )
	{
		Authorized.Remove( player.SteamId );
	}

	public bool IsAuthorized( ForsakenPlayer player )
	{
		return Authorized.Contains( player.SteamId );
	}

	public bool IsAuthorized()
	{
		Game.AssertClient();
		return Authorized.Contains( Game.LocalClient.SteamId );
	}

	public string GetContextName()
	{
		return "Door";
	}

	public void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( Game.IsClient ) return;

		if ( action == OpenAction && IsAuthorized( player ) )
		{
			PlaySound( "door.single.open" );
			IsOpen = true;
		}
		else if ( action == CloseAction && IsAuthorized( player ) )
		{
			PlaySound( "door.single.close" );
			IsOpen = false;
		}
		else if ( action == LockAction && IsAuthorized( player ) )
		{
			UI.LockScreen.OpenToLock( player, this );
		}
		else if ( action == AuthorizeAction )
		{
			UI.LockScreen.OpenToUnlock( player, this );
		}
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/single_door.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "hover", "solid", "door" );
	}

	public override void OnPlacedByPlayer( ForsakenPlayer player )
	{
		Authorize( player );
		base.OnPlacedByPlayer( player );
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
		if ( Game.IsServer || IsClientOnly )
		{
			Socket = AddSocket( "center" );
			Socket.ConnectAny.Add( "doorway" );
			Socket.Tags.Add( "door" );
		}

		base.OnNewModel( model );
	}

	public override void SerializeState( BinaryWriter writer )
	{
		writer.Write( IsOpen );
		writer.Write( IsLocked );
		writer.Write( string.IsNullOrEmpty( Code ) ? "" : Code );
		writer.Write( Authorized.Count );

		foreach ( var id in Authorized )
		{
			writer.Write( id );
		}

		base.SerializeState( writer );
	}

	public override void DeserializeState( BinaryReader reader )
	{
		IsOpen = reader.ReadBoolean();
		IsLocked = reader.ReadBoolean();
		Code = reader.ReadString();

		var count = reader.ReadInt32();

		for ( var i = 0; i < count; i++ )
		{
			var id = reader.ReadInt64();
			Authorized.Add( id );
		}

		base.DeserializeState( reader );
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
