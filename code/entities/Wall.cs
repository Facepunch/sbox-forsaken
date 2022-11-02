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

	public override void OnNewModel( Model model )
	{
		if ( IsServer || IsClientOnly )
		{
			var socket = AddSocket( "center" );
			socket.ConnectAny.Add( "foundation" );
			socket.Tags.Add( "wall" );
		}

		base.OnNewModel( model );
	}
}
