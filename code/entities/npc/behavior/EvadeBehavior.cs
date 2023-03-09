using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class EvadeBehavior : EntityComponent
{
	public float MaxPrediction { get; set; } = 1f;

	private SteeringComponent Steering { get; set; }
	private FleeBehavior Flee { get; set; }

	protected override void OnActivate()
	{
		Steering = Entity.Components.GetOrCreate<SteeringComponent>();
		Flee = Entity.Components.GetOrCreate<FleeBehavior>();
	}

	public Vector3 GetSteering( Entity target )
	{
		var displacement = target.Position - Entity.Position;
		var distance = displacement.Length;
		var speed = target.Velocity.Length;

		float prediction;
		if ( speed <= distance / MaxPrediction )
		{
			prediction = MaxPrediction;
		}
		else
		{
			prediction = distance / speed;
			prediction *= 0.9f;
		}

		var explicitTarget = target.Position + target.Velocity * prediction;
		return Flee.GetSteering( explicitTarget );
	}
}
