using Sandbox;
using System.Linq;
using System.Net.Sockets;

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

		Tags.Add( "solid", "foundation" );
	}

	public override bool IsValidPlacement( Vector3 target )
	{
		return !FindInSphere( target, 512f )
			.OfType<Foundation>()
			.Where( s => !s.Equals( this ) )
			.Any();
	}

	public override void OnNewModel( Model model )
	{
		if ( IsServer || IsClientOnly )
		{
			AddFoundationSocket( "forward", "backward" );
			AddFoundationSocket( "backward", "forward" );
			AddFoundationSocket( "left", "right" );
			AddFoundationSocket( "right", "left" );

			AddWallSocket( "forward" );
			AddWallSocket( "backward" );
			AddWallSocket( "left" );
			AddWallSocket( "right" );
		}

		base.OnNewModel( model );
	}

	private void AddFoundationSocket( string direction, string connectorDirection )
	{
		var socket = AddSocket( direction );
		socket.ConnectAny.Add( "foundation" );
		socket.ConnectAll.Add( connectorDirection );
		socket.Tags.Add( "foundation", direction );
	}

	private void AddWallSocket( string attachmentName )
	{
		var socket = AddSocket( attachmentName );
		socket.Tags.Add( "foundation", "wall" );
	}
}
