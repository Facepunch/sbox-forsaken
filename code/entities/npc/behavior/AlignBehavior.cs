using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class AlignBehavior : EntityComponent
{
	public float FacingAngle { get; set; } = 90f;
	public float InterpolationTime { get; set; } = 0.1f;
	public float MaxAcceleration { get; set; } = 10f;

	private SteeringComponent Steering { get; set; }
	private float CosineThreshold { get; set; }

	protected override void OnActivate()
	{
		Steering = Entity.Components.GetOrCreate<SteeringComponent>();
		CosineThreshold = MathF.Cos( FacingAngle.DegreeToRadian() );
	}

	public Vector3 GetSteering( IEnumerable<Entity> flock )
	{
		var acceleration = Vector3.Zero;
		var entityCount = 0;

		foreach ( var them in flock )
		{
			if ( them == Entity ) continue;

			if ( Steering.IsFacing( them.Position, CosineThreshold ) )
			{
				var a = them.Velocity - Entity.Velocity;
				a /= InterpolationTime;

				acceleration += a;
				entityCount++;
			}
		}

		if ( entityCount > 0 )
		{
			acceleration = acceleration / entityCount;

			if ( acceleration.Length > MaxAcceleration )
				acceleration = acceleration.Normal * MaxAcceleration;
		}

		return acceleration;
	}
}
