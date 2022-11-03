using Sandbox;

namespace Facepunch.Forsaken;

public partial class StorageCrate : Deployable
{
	public override void Spawn()
	{
		SetModel( "models/citizen_props/crate01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		base.Spawn();
	}
}
