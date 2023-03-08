using System;
using System.Collections.Generic;
using Sandbox;

namespace Facepunch.Forsaken;

public class SteeringComponent : EntityComponent
{
	public float MaxVelocity { get; set; } = 200f;
	public float MaxAcceleration { get; set; } = 30f;
	public float RotateSpeed { get; set; } = 2f;

	public float TargetDistance { get; set; } = 8f;
	public float SlowDistance { get; set; } = 64f;
	public float InterpolationTime { get; set; } = 0.1f;

	public bool EnableSmoothing { get; set; } = true;
	public int SmoothingSamples { get; set; } = 5;

	private Queue<Vector3> SampleQueue { get; set; } = new();

	public void Steer( Vector3 acceleration )
	{
		Entity.Velocity += acceleration;

		if ( Entity.Velocity.Length > MaxVelocity )
		{
			Entity.Velocity = Entity.Velocity.Normal * MaxVelocity;
		}
	}

	public Vector3 Seek( Vector3 position, float accelerationOverride )
	{
		DebugOverlay.Sphere( position, 32f, Color.Blue );
		var acceleration = (position - Entity.Position).Normal;
		acceleration *= accelerationOverride;
		DebugOverlay.Line( Entity.Position + Vector3.Up * 16f, Entity.Position + acceleration, Color.Cyan );
		return acceleration;
	}

	public Vector3 Seek( Vector3 position )
	{
		return Seek( position, MaxAcceleration );
	}

	public void RotateToTarget()
	{
		var direction = Entity.Velocity;

		if ( EnableSmoothing )
		{
			if ( SampleQueue.Count == SmoothingSamples )
			{
				SampleQueue.Dequeue();
			}

			SampleQueue.Enqueue( Entity.Velocity );

			direction = Vector3.Zero;

			foreach ( var v in SampleQueue )
			{
				direction += v;
			}

			direction /= SampleQueue.Count;
		}

		LookAtDirection( direction );
	}

	public void LookAtDirection( Vector3 direction )
	{
		direction = direction.Normal;

		if ( direction.LengthSquared > 0.001f )
		{
			var targetRotation = Rotation.LookAt( direction.WithZ( 0f ), Vector3.Up );
			Entity.Rotation = Rotation.Lerp( Entity.Rotation, targetRotation, Time.Delta * RotateSpeed );
		}
	}

	public Vector3 Arrive( Vector3 targetPosition )
	{
		var targetVelocity = targetPosition - Entity.Position;
		var distance = targetVelocity.Length;

		if ( distance < TargetDistance )
		{
			Entity.Velocity = Vector3.Zero;
			return Vector3.Zero;
		}

		float targetSpeed;
		if ( distance > SlowDistance )
			targetSpeed = MaxVelocity;
		else
			targetSpeed = MaxVelocity * (distance / SlowDistance);

		targetVelocity = targetVelocity.Normal;
		targetVelocity *= targetSpeed;

		var acceleration = targetVelocity - Entity.Velocity;
		acceleration *= 1 / InterpolationTime;

		if ( acceleration.Length > MaxAcceleration )
		{
			acceleration = acceleration.Normal;
			acceleration *= MaxAcceleration;
		}

		return acceleration;
	}

	public Vector3 Interpose( Entity a, Entity b )
	{
		var midPoint = (a.Position + b.Position) / 2;

		float timeToReachMidPoint = Vector3.DistanceBetween( midPoint, Entity.Position ) / MaxVelocity;

		var futurePosA = a.Position + a.Velocity * timeToReachMidPoint;
		var futurePosB = b.Position + b.Velocity * timeToReachMidPoint;

		midPoint = (futurePosA + futurePosB) / 2f;

		return Arrive( midPoint );
	}

	public bool IsInFront( Vector3 target )
	{
		return IsFacing( target, 0f );
	}

	public bool IsFacing( Vector3 target, float threshold )
	{
		var facing = Entity.Rotation.Forward.Normal;
		var direction = (target - Entity.Position).Normal;
		return Vector3.Dot( facing, direction ) >= threshold;
	}

	public static Vector3 OrientationToVector( float orientation )
	{
		return new Vector3( MathF.Sin( -orientation ), MathF.Cos( -orientation ), 0f );
	}

	public static float VectorToOrientation( Vector3 direction )
	{
		return -1 * MathF.Atan2( direction.x, direction.y );
	}
}
