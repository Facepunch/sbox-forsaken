using Sandbox;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class Structure : ModelEntity
{
	public static Structure Ghost { get; private set; }

	public static Structure GetOrCreateGhost( TypeDescription type )
	{
		if ( !Ghost.IsValid() || type.GetType() != Ghost.GetType() )
		{
			ClearGhost();

			Ghost = type.Create<Structure>();
			Ghost.EnableShadowCasting = false;
			Ghost.EnableShadowReceive = false;
			Ghost.EnableAllCollisions = false;
			Ghost.SetMaterialOverride( Material.Load( "materials/blueprint.vmat" ) );
		}

		return Ghost;
	}

	public static void ClearGhost()
	{
		Ghost?.Delete();
		Ghost = null;
	}

	[Net] public Socket Socket { get; internal set; }
	[Net] public IList<Socket> Sockets { get; set; } = new List<Socket>();

	public virtual bool RequiresSocket => true;
	public virtual bool ShouldRotate => true;

	public void SnapToSocket( Socket.Match match )
	{
		var transform = match.Theirs.Transform;

		// TODO: This is pretty shitty. I'm sure there's a better way.
		Rotation = Rotation.Identity;
		var relative = Position - match.Ours.Position;

		Position = transform.Position + relative;

		if ( ShouldRotate )
			Rotation = transform.Rotation;

		ResetInterpolation();
	}

	public virtual void OnConnected( Socket ours, Socket theirs )
	{

	}

	public virtual bool CanConnectTo( Socket socket )
	{
		return true;
	}

	public virtual bool IsValidPlacement( Vector3 target, Vector3 normal )
	{
		return normal.Dot( Vector3.Up ).AlmostEqual( 1f );
	}

	public virtual Socket.Match LocateSocket( Vector3 target )
	{
		var ourSockets = Sockets
			.OrderBy( s => s.Position.Distance( target ) );

		var nearbyStructures = FindInSphere( target, 48f )
			.OfType<Structure>()
			.Where( s => !s.Equals( this ) )
			.OrderBy( s => s.Position.Distance( target ) );

		foreach ( var theirStructure in nearbyStructures )
		{
			var theirSockets = theirStructure.Sockets
				.Where( s => !s.Connection.IsValid() && CanConnectTo( s ) )
				.OrderBy( a => a.Position.Distance( target ) );

			foreach ( var theirSocket in theirSockets )
			{
				foreach ( var ourSocket in ourSockets )
				{
					if ( ourSocket.CanConnectTo( theirSocket ) )
					{
						return new Socket.Match( ourSocket, theirSocket );
					}
				}
			}
		}

		return default;
	}

	protected Socket AddSocket( string attachmentName )
	{
		var attachment = GetAttachment( attachmentName );

		if ( attachment.HasValue )
		{
			var socket = new Socket
			{
				Transform = attachment.Value
			};

			socket.SetParent( this );

			AddSocket( socket );

			return socket;
		}

		return null;
	}

	protected Socket AddSocket( Socket socket )
	{
		socket.SetParent( this );
		Sockets.Add( socket );
		return socket;
	}
}
