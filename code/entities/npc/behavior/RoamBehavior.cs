using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class RoamBehavior : EntityComponent
{
	public bool CheckCollisions { get; set; } = true;
	public float RoamOffset { get; set; } = 10f;
	public float RoamRadius { get; set; } = 250f;
	public float RoamRate { get; set; } = 0.2f;

	private SteeringComponent Steering { get; set; }
	private float Orientation { get; set; }

	protected override void OnActivate()
	{
		Steering = Entity.Components.GetOrCreate<SteeringComponent>();
	}

	public Vector3 GetSteering()
	{
		var entityOrientation = SteeringComponent.VectorToOrientation( Entity.Rotation.Forward ) * -1f;
		var binomial = Game.Random.Float() - Game.Random.Float();

		Orientation += binomial * RoamRate;

		RoamOffset = 10f;
		RoamRadius = 250f;
		RoamRate = 0.2f;

		var origin = Entity.Position + Vector3.Up * 20f;
		var targetOrientation = Orientation + entityOrientation;
		var targetPosition = origin + (SteeringComponent.OrientationToVector( entityOrientation ) * RoamOffset);
		targetPosition += (SteeringComponent.OrientationToVector( targetOrientation ) * RoamRadius);

		if ( CheckCollisions )
		{
			var trace = Trace.Ray( origin, targetPosition )
				.WorldAndEntities()
				.WithoutTags( "trigger", "passplayers" )
				.WithAnyTags( "solid" )
				.Ignore( Entity )
				.Run();

			if ( NPC.Debug )
				DebugOverlay.Line( trace.StartPosition, trace.EndPosition, Color.Magenta );

			if ( trace.Hit || trace.StartedSolid )
			{
				return Steering.Seek( trace.HitPosition - trace.Direction * RoamRadius );
			}
		}

		return Steering.Seek( targetPosition );
	}
}
