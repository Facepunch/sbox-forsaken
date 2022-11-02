using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

[Title( "Foundation" )]
[Description( "The most fundamental building block. Walls, doors and windows can be attached to it." )]
[Icon( "textures/ui/foundation.png" )]
public partial class Foundation : Structure
{
	public override bool RequiresSocket => false;
	public override bool ShouldRotate => false;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/foundation.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	public override void OnNewModel( Model model )
	{
		if ( IsServer || IsClientOnly )
		{
			var socket = AddSocket( "forward" );
			socket.ConnectAny.Add( "foundation" );
			socket.ConnectAll.Add( "backward" );
			socket.Tags.Add( "foundation", "forward" );

			socket = AddSocket( "backward" );
			socket.ConnectAny.Add( "foundation" );
			socket.ConnectAll.Add( "forward" );
			socket.Tags.Add( "foundation", "backward" );

			socket = AddSocket( "left" );
			socket.ConnectAny.Add( "foundation" );
			socket.ConnectAll.Add( "right" );
			socket.Tags.Add( "foundation", "left" );

			socket = AddSocket( "right" );
			socket.ConnectAny.Add( "foundation" );
			socket.ConnectAll.Add( "left" );
			socket.Tags.Add( "foundation", "right" );
		}

		base.OnNewModel( model );
	}
}
