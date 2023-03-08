using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class SeparationBehavior : EntityComponent
{
	public float MaxAcceleration { get; set; } = 8f;
	public float MaxDistance { get; set; } = 30f;

	public Vector3 GetSteering( IEnumerable<Entity> flock )
	{
		var acceleration = Vector3.Zero;
		var ourSize = 20f;

		foreach ( var them in flock )
		{
			if ( them == Entity ) continue;

			var direction = (them.Position - Entity.Position);
			var theirSize = 20f;
			var distance = direction.Length;

			if ( distance < MaxDistance )
			{
				var strength = MaxAcceleration * (MaxDistance - distance) / (MaxDistance - ourSize - theirSize);
				acceleration += direction.Normal * strength;
			}
		}

		return acceleration;
	}
}
