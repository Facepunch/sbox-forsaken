using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

[Title( "Doorway" )]
[Description( "Can have a door placed inside. Must be placed on a foundation." )]
[Icon( "textures/ui/doorway.png" )]
public partial class Doorway : Structure
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/doorway.vmdl" );
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
