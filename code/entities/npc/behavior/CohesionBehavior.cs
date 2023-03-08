using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class CohesionBehavior : EntityComponent
{
	public float FacingAngle { get; set; } = 120f;

	private SteeringComponent Steering { get; set; }
	private float CosineThreshold { get; set; }

	protected override void OnActivate()
	{
		Steering = Entity.Components.GetOrCreate<SteeringComponent>();
		CosineThreshold = MathF.Cos( FacingAngle.DegreeToRadian() );
	}

	public Vector3 GetSteering( IEnumerable<Entity> flock )
	{
		var centerOfMass = Vector3.Zero;
		var entityCount = 0;

		foreach ( var them in flock )
		{
			if ( them == Entity ) continue;

			if ( Steering.IsFacing( them.Position, CosineThreshold ) )
			{
				centerOfMass += them.Position;
				entityCount++;
			}
		}

		if ( entityCount == 0 ) return Vector3.Zero;

		centerOfMass /= entityCount;

		return Steering.Arrive( centerOfMass );
	}
}
