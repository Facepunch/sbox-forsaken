using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public class AvoidanceBehavior : EntityComponent
{
	public HashSet<string> ObstacleTags { get; set; } = new();

	public float MaxAcceleration { get; set; } = 60f;
	public float MinimumDistance { get; set; } = 80f;
	public float MaxStandableAngle { get; set; } = 20f;

	public float MainWhiskerLength { get; set; } = 45;
	public float SideWhiskerLength { get; set; } = 25f;
	public float SideWhiskerAngle { get; set; } = 45f;

	private SteeringComponent Steering { get; set; }

	protected override void OnActivate()
	{
		Steering = Entity.Components.GetOrCreate<SteeringComponent>();
	}

	public Vector3 GetSteering()
	{
		var velocity = Entity.Velocity.WithZ( 0f );

		if ( velocity.Length > 0.005f )
			return GetSteering( velocity );
		else
			return GetSteering( Entity.Rotation.Forward );
	}

	public Vector3 GetSteering( Vector3 facingDir )
	{
		var acceleration = Vector3.Zero;
		TraceResult trace;

		if ( !FindObstacle( facingDir, out trace ) )
		{
			return acceleration;
		}

		var targetPostition = trace.HitPosition + Vector3.Reflect( trace.Direction, trace.Normal ) * MinimumDistance;
		float angle = Vector3.GetAngle( Entity.Velocity, trace.Normal );

		if ( angle > 165f )
		{
			//Vector3 perp = new Vector3( -trace.Normal.z, trace.Normal.y, trace.Normal.x );
			//targetPostition += (perp * MathF.Sin( (angle - 165f).DegreeToRadian() ) * 2f * MinimumDistance);
		}

		return Steering.Seek( targetPostition, MaxAcceleration );
	}

	private bool FindObstacle( Vector3 facingDir, out TraceResult result )
	{
		facingDir = facingDir.Normal;

		var dirs = new Vector3[3];
		dirs[0] = facingDir;

		var orientation = SteeringComponent.VectorToOrientation( facingDir );
		dirs[1] = SteeringComponent.OrientationToVector( orientation + SideWhiskerAngle.DegreeToRadian() );
		dirs[2] = SteeringComponent.OrientationToVector( orientation - SideWhiskerAngle.DegreeToRadian() );

		return CastWhiskers( dirs, out result );
	}

	private bool CastWhiskers( Vector3[] dirs, out TraceResult result )
	{
		result = default;

		var obstacleTagsArray = ObstacleTags.ToArray();
		var didHitAnything = false;
		var origin = Entity.Position + Vector3.Up * 12f;

		for ( int i = 0; i < dirs.Length; i++ )
		{
			var distance = i == 0 ? MainWhiskerLength : SideWhiskerLength;
			var trace = Trace.Ray( origin, origin + dirs[i] * distance )
				.WorldAndEntities()
				.WithoutTags( "passplayers" )
				.WithAnyTags( "solid" )
				.WithAnyTags( obstacleTagsArray )
				.Ignore( Entity )
				.Size( 8f )
				.Run();

			if ( trace.Hit )
			{
				DebugOverlay.Line( origin, origin + dirs[i] * distance, Entity.Velocity.IsNearZeroLength ? Color.Cyan : Color.Green );

				if ( trace.Normal.Angle( Vector3.Up ) > MaxStandableAngle )
				{
					didHitAnything = true;
					result = trace;
					break;
				}
			}
			else
			{
				DebugOverlay.Line( origin, origin + dirs[i] * distance, Color.Red );
			}
		}

		return didHitAnything;
	}
}
