using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

[Title( "Foundation" )]
[Description( "The most fundamental building block. Walls, doors and windows can be attached to it." )]
public partial class Foundation : Structure
{
	public override void Spawn()
	{
		SetModel( "models/structures/foundation.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		base.Spawn();
	}

	public override void OnNewModel( Model model )
	{
		AddSocket( "forward" );
		AddSocket( "backward" );
		AddSocket( "left" );
		AddSocket( "right" );

		base.OnNewModel( model );
	}

	public override bool LocateSlot( Vector3 target, out Vector3 position, out Rotation rotation )
	{
		position = target;
		rotation = Rotation.Identity;

		var foundations = FindInSphere( target, 64f ).OfType<Foundation>();

		if ( foundations.Any() )
		{
			var targetFoundation = foundations.FirstOrDefault();
			var orderedSockets = targetFoundation.Sockets.OrderBy( a =>
			{
				var transform = targetFoundation.Transform.ToWorld( a.LocalTransform );
				return transform.Position.Distance( target );
			} );
			var targetSocket = orderedSockets.FirstOrDefault();

			var transform = targetFoundation.Transform.ToWorld( targetSocket.LocalTransform );

			position = transform.Position;
			rotation = transform.Rotation;

			return true;
		}

		return true;
	}
}
