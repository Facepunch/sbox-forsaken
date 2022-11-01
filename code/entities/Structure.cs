using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class Structure : ModelEntity
{
	[Net] public Socket Socket { get; internal set; }
	[Net] public IList<Socket> Sockets { get; set; } = new List<Socket>();

	public virtual bool RequiresSocket => true;

	public virtual bool LocateSocket( Vector3 target, out Socket socket )
	{
		socket = null;
		return false;
	}

	protected void AddSocket( string attachmentName )
	{
		Host.AssertServer();

		var attachment = GetAttachment( attachmentName );

		if ( attachment.HasValue )
		{
			var socket = new Socket
			{
				Transform = attachment.Value
			};

			socket.SetParent( this );

			AddSocket( socket );
		}
	}

	protected void AddSocket( Socket socket )
	{
		Host.AssertServer();

		socket.SetParent( this );

		Sockets.Add( socket );
	}

	protected float OrderStructureByDistance( Vector3 target, Structure structure )
	{
		return structure.Position.Distance( target );
	}

	protected float OrderSocketByDistance( Vector3 target, Socket socket )
	{
		return socket.Position.Distance( target );
	}
}
