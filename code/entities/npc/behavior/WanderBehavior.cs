using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class WanderBehavior : EntityComponent
{
	public float WanderRadius { get; set; } = 256f;
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

		var targetPosition = Entity.Position + Entity.Rotation.Forward * WanderDistance + CurrentTarget;
		return Steering.Seek( targetPosition );
	}
}
