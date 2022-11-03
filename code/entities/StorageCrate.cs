using Sandbox;

namespace Facepunch.Forsaken;

public partial class StorageCrate : Deployable, IContextActions
{
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	public override void Spawn()
	{
		SetModel( "models/citizen_props/crate01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		base.Spawn();
	}
}
