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

	public override void OnNewModel( Model model )
	{
		if ( IsServer || IsClientOnly )
		{
			var socket = AddSocket( "center" );
			socket.ConnectAny.Add( "foundation" );
			socket.Tags.Add( "doorway" );
		}

		base.OnNewModel( model );
	}
}
