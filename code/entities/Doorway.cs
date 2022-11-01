using Sandbox;

namespace Facepunch.Forsaken;

[Title( "Doorway" )]
[Description( "Can have a door placed inside. Must be placed on a foundation." )]
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
		if ( IsServer )
		{
			AddSocket( "center" );
		}

		base.OnNewModel( model );
	}
}
