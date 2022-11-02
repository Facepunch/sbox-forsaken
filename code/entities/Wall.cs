using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

[Title( "Wall" )]
[Description( "Prevents anything getting in or out. Must be attached to a foundation." )]
[Icon( "textures/ui/wall.png" )]
public partial class Wall : Structure
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/wall.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	public override bool LocateSocket( Vector3 target, out Socket socket )
	{
		socket = null;

		var nearest = FindInSphere( target, 64f ).OfType<Foundation>();
		if ( !nearest.Any() ) return false;

		var structures = nearest.OrderBy( s => OrderStructureByDistance( target, s ) );

		foreach ( var structure in structures )
		{
			var orderedSockets = structure.Sockets
				.Where( s => s.Structures.Count == 0 )
				.OrderBy( a => OrderSocketByDistance( target, a ) );

			var foundSocket = orderedSockets.FirstOrDefault();

			if ( foundSocket.IsValid() )
			{
				socket = foundSocket;
				return true;
			}
		}

		return false;
	}

	public override void OnNewModel( Model model )
	{
		if ( IsServer )
		{
			AddSocket( "center" );
		}

		base.OnNewModel( model );
	}
}
