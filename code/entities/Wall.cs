using Sandbox;

namespace Facepunch.Forsaken;

[Title( "Wall" )]
[Description( "Prevents anything getting in or out. Must be attached to a foundation." )]
public partial class Wall : Structure
{
	public override void Spawn()
	{
		SetModel( "models/structures/wall.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		base.Spawn();
	}

	public override void OnNewModel( Model model )
	{
		AddSocket( "center" );

		base.OnNewModel( model );
	}
}
