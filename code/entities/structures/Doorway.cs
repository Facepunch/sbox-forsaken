using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

[Title( "Doorway" )]
[Description( "Can have a door placed inside. Must be placed on a foundation." )]
[Icon( "textures/ui/doorway.png" )]
[ItemCost( "wood", 10 )]
public partial class Doorway : Structure
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/doorway.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "solid", "wall", "doorway" );
	}

	public override bool CanConnectTo( Socket socket )
	{
		return !FindInSphere( socket.Position, 32f )
			.OfType<Structure>()
			.Where( s => !s.Equals( this ) )
			.Where( s => s.Tags.Has( "wall" ) )
			.Any();
	}

	public override void OnNewModel( Model model )
	{
		if ( IsServer || IsClientOnly )
		{
			var socket = AddSocket( "center" );
			socket.ConnectAny.Add( "wall" );
			socket.Tags.Add( "doorway" );
		}

		base.OnNewModel( model );
	}
}
