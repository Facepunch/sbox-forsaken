using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class GravityComponent : EntityComponent
{
	public Entity GroundEntity { get; private set; }
	public float Force { get; set; } = 700f;

	public void Update()
	{
		var trace = Trace.Ray( Entity.Position + Vector3.Up * 8f, Entity.Position + Vector3.Down * 32f )
			.WorldAndEntities()
			.WithAnyTags( "solid" )
			.WithoutTags( "trigger", "passplayers" )
			.Ignore( Entity )
			.Run();

		if ( trace.Hit )
			Entity.GroundEntity = trace.Entity;
		else
			Entity.GroundEntity = null;

		Entity.Velocity += Vector3.Down * Force * Time.Delta;
	}
}
