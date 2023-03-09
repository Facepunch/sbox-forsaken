using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class FleeBehavior : EntityComponent
{
	public float PanicDistance { get; set; } = 140f;
	public bool DecelerateOnStop { get; set; } = true;
	public float MaxAcceleration { get; set; } = 10f;
	public float InterpolationTime { get; set; } = 0.1f;

	private SteeringComponent Steering { get; set; }

	protected override void OnActivate()
	{
		Steering = Entity.Components.GetOrCreate<SteeringComponent>();
	}

	public Vector3 GetSteering( Vector3 target )
	{
		var acceleration = Entity.Position - target;

		if ( acceleration.Length > PanicDistance )
		{
			if ( DecelerateOnStop && !Entity.Velocity.WithZ( 0f ).IsNearZeroLength )
			{
				acceleration = -Entity.Velocity / InterpolationTime;

				if ( acceleration.Length > MaxAcceleration )
				{
					acceleration = acceleration.Normal * MaxAcceleration;
				}

				return acceleration;
			}
			else
			{
				Entity.Velocity = Vector3.Zero;
				return Vector3.Zero;
			}
		}

		return acceleration.Normal * MaxAcceleration;
	}
}
