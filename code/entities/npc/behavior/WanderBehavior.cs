using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class WanderBehavior : EntityComponent
{
	public bool CheckCollisions { get; set; } = true;
	public float CollisionRange { get; set; } = 45f;
	public float WanderRadius { get; set; } = 512f;
	public float WanderDistance { get; set; } = 24f;
	public float WanderJitter { get; set; } = 16f;

	private SteeringComponent Steering { get; set; }
	private Vector3 CurrentTarget { get; set; }

	protected override void OnActivate()
	{
		Steering = Entity.Components.GetOrCreate<SteeringComponent>();
		Regenerate();
	}

	public void Regenerate()
	{
		var theta = Game.Random.Float() * 2f * MathF.PI;
		CurrentTarget = new Vector3( WanderRadius * MathF.Cos( theta ), WanderRadius * MathF.Sin( theta ), 0f );
	}

	public Vector3 GetSteering()
	{
		var jitter = WanderJitter;
		CurrentTarget += new Vector3( Game.Random.Float( -1f, 1f ) * jitter, Game.Random.Float( -1f, 1f ) * jitter, 0f );
		CurrentTarget = CurrentTarget.Normal;
		CurrentTarget *= WanderRadius;

		var origin = Entity.Position + Vector3.Up * 20f;
		var targetPosition = origin + Entity.Rotation.Forward * WanderDistance + CurrentTarget;

		if ( CheckCollisions )
		{
			var direction = (targetPosition - origin).Normal;
			var trace = Trace.Ray( origin, origin + direction * CollisionRange )
				.WorldAndEntities()
				.WithoutTags( "trigger", "passplayers" )
				.WithAnyTags( "solid" )
				.Ignore( Entity )
				.Run();

			if ( NPC.Debug )
				DebugOverlay.Line( trace.StartPosition, trace.EndPosition, Color.Magenta );

			if ( trace.Hit || trace.StartedSolid )
			{
				var reflection = Vector3.Reflect( trace.Direction, trace.Normal );
				CurrentTarget = CurrentTarget.LerpTo( reflection * WanderRadius, Time.Delta * WanderRadius * 0.1f );
				CurrentTarget = CurrentTarget.WithZ( 0f );
			}
		}

		return Steering.Seek( targetPosition );
	}
}
