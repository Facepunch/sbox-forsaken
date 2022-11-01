using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class Structure : ModelEntity
{
	public IReadOnlyList<Socket> Sockets => InternalSockets;
	private List<Socket> InternalSockets { get; set; } = new();

	public override void Spawn()
	{
		base.Spawn();
	}

	public virtual bool LocateSlot( Vector3 target, out Vector3 position, out Rotation rotation )
	{
		position = target;
		rotation = Rotation.Identity;

		var nearest = FindInSphere( target, 64f ).OfType<Structure>();

		if ( nearest.Any() )
		{
			var targetStructure = nearest.FirstOrDefault();
			var orderedSockets = targetStructure.Sockets.OrderBy( a =>
			{
				var transform = targetStructure.Transform.ToWorld( a.LocalTransform );
				return transform.Position.Distance( target );
			} );
			var targetSocket = orderedSockets.FirstOrDefault();

			var transform = targetStructure.Transform.ToWorld( targetSocket.LocalTransform );

			position = transform.Position;
			rotation = transform.Rotation;

			return true;
		}

		return false;
	}

	protected void AddSocket( string attachmentName )
	{
		var attachment = GetAttachment( attachmentName, false );

		if ( attachment.HasValue )
		{
			var socket = new Socket
			{
				LocalTransform = attachment.Value
			};

			AddSocket( socket );
		}
	}

	protected void AddSocket( Socket socket )
	{
		InternalSockets.Add( socket );
	}
}
