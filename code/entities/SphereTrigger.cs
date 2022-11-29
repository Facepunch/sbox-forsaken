using Sandbox;

namespace Facepunch.Forsaken;

public class SphereTrigger : ModelEntity
{
	public static void Attach( Entity entity, float radius )
	{
		var trigger = new SphereTrigger();
		trigger.SetParent( entity );
		trigger.SetRadius( radius );
		trigger.Position = entity.Position;
	}

	public void SetRadius( float radius )
	{
		SetupPhysicsFromSphere( PhysicsMotionType.Keyframed, Vector3.Zero, radius );
		EnableTraceAndQueries = false;
		EnableTouch = true;
		Tags.Add( "trigger" );
	}
}
