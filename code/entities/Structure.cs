using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class Structure : ModelEntity
{
	[Net] public Socket Socket { get; internal set; }
	[Net] public List<Socket> Sockets { get; set; }

	public virtual bool RequiresSocket => true;

	public override void Spawn()
	{
		Sockets = new List<Socket>();

		base.Spawn();
	}

	public virtual bool LocateSocket( Vector3 target, out Socket socket )
	{
		var nearest = FindInSphere( target, 64f ).OfType<Structure>();

		if ( nearest.Any() )
		{
			var structures = nearest.OrderBy( s => OrderStructureByDistance( target, s ) );

			foreach ( var structure in structures )
			{
				var orderedSockets = structure.Sockets
					.Where( s => s.Structures.Count == 0 )
					.OrderBy( a => OrderSocketByDistance( target, a ) );

				socket = orderedSockets.FirstOrDefault();

				if ( socket.IsValid() )
				{
					return true;
				}
			}
		}

		socket = null;

		return false;
	}

	protected void AddSocket( string attachmentName )
	{
		Host.AssertServer();

		var attachment = GetAttachment( attachmentName, false );

		if ( attachment.HasValue )
		{
			var socket = new Socket( this )
			{
				LocalTransform = attachment.Value
			};

			AddSocket( socket );
		}
	}

	protected void AddSocket( Socket socket )
	{
		Host.AssertServer();

		Sockets.Add( socket );
	}

	protected float OrderStructureByDistance( Vector3 target, Structure structure )
	{
		return structure.Position.Distance( target );
	}

	protected float OrderSocketByDistance( Vector3 target, Socket socket )
	{
		var transform = socket.Owner.Transform.ToWorld( socket.LocalTransform );
		return transform.Position.Distance( target );
	}
}
