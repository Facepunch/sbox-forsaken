using Sandbox;

namespace Facepunch.Forsaken;

public abstract partial class NPC : AnimatedEntity
{
	protected virtual bool UseMoveHelper => false;

	protected Vector3 TargetLocation { get; set; }
	protected TimeUntil NextWanderTime { get; set; }
	protected Vector3 WishDirection { get; set; }
	protected NavPath Path { get; set; }

	public override void Spawn()
	{
		Tags.Add( "npc" );

		base.Spawn();
	}

	public virtual bool ShouldWander()
	{
		return false;
	}

	public virtual string GetDisplayName()
	{
		return "NPC";
	}

	public virtual float GetMoveSpeed()
	{
		return 80f;
	}

	public virtual float GetIdleDuration()
	{
		return 30f;
	}

	protected void SnapToNavMesh()
	{
		var closest = NavMesh.GetClosestPoint( Position );

		if ( closest.HasValue )
			Position = closest.Value;
	}

	protected bool MoveToLocation( Vector3 position, float stepSize = 16f )
	{
		TargetLocation = position;

		Path = NavMesh.PathBuilder( Position )
			.WithAgentHull( NavAgentHull.Default )
			.WithPartialPaths()
			.WithStepHeight( stepSize )
			.Build( TargetLocation );

		return (Path?.Count ?? 0) > 0;
	}

	protected bool TryGetNavMeshPosition( float minRadius, float maxRadius, out Vector3 position )
	{
		var targetPosition = NavMesh.GetPointWithinRadius( Position, minRadius, maxRadius );

		if ( targetPosition.HasValue )
		{
			position = targetPosition.Value;
			return true;
		}

		position = default;
		return false;
	}

	[Event.Tick.Server]
	protected virtual void ServerTick()
	{
		if ( LifeState == LifeState.Dead )
		{
			Velocity = Vector3.Zero;
			HandleAnimation();
			return;
		}

		if ( ShouldWander() && NextWanderTime && NavMesh.IsLoaded )
		{
			SnapToNavMesh();

			if ( TryGetNavMeshPosition( 100f, 5000f, out var targetPosition ) )
			{
				MoveToLocation( targetPosition );
				NextWanderTime = GetIdleDuration();
			}
		}

		var hull = GetHull();
		var pm = TraceBBox( Position + Vector3.Up * 8f, Position + Vector3.Down * 32f, hull.Mins, hull.Maxs );

		GroundEntity = pm.Entity;

		if ( !GroundEntity.IsValid() )
		{
			Velocity += Vector3.Down * 700f * Time.Delta;
		}
		else
		{
			Position = Position.WithZ( pm.EndPosition.z );
			Velocity = Velocity.WithZ( 0f );
			Velocity = ApplyFriction( Velocity, 4f );
		}

		var wishDirection = GetWishDirection();
		Velocity = Accelerate( Velocity, wishDirection, GetMoveSpeed(), 0f, 8f );

		if ( wishDirection.Length > 0f )
		{
			var targetRotation = Rotation.LookAt( wishDirection, Vector3.Up );
			Rotation = Rotation.Lerp( Rotation, targetRotation, Time.Delta * 10f );
		}

		HandleAnimation();

		if ( UseMoveHelper )
		{
			var mover = new MoveHelper( Position, Velocity );

			mover.Trace = mover.SetupTrace()
				.WithoutTags( "passplayers", "player" )
				.WithAnyTags( "solid", "playerclip", "passbullets" )
				.Size( GetHull() )
				.Ignore( this );

			mover.MaxStandableAngle = 40f;
			mover.TryMoveWithStep( Time.Delta, 24f );

			Position = mover.Position;
			Velocity = mover.Velocity;
		}
		else
		{
			Position += Velocity * Time.Delta;
		}
	}

	protected virtual Vector3 ApplyFriction( Vector3 velocity, float amount = 1f )
	{
		var speed = Velocity.Length;
		if ( speed < 0.1f ) return velocity;

		var control = (speed < 100f) ? 100f : speed;
		var newSpeed = speed - (control * Time.Delta * amount);

		if ( newSpeed < 0 ) newSpeed = 0;
		if ( newSpeed == speed ) return velocity;

		newSpeed /= speed;
		velocity *= newSpeed;

		return velocity;
	}

	protected virtual void HandleAnimation()
	{

	}

	protected virtual TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{
		var trace = Trace.Ray( start, end )
			.Size( mins, maxs )
			.WithoutTags( "passplayers", "trigger" )
			.WithAnyTags( "solid" )
			.Ignore( this )
			.Run();

		return trace;
	}

	protected virtual BBox GetHull()
	{
		var girth = 16f;
		var mins = new Vector3( -girth, -girth, 0f );
		var maxs = new Vector3( +girth, +girth, 72f );
		return new BBox( mins, maxs );
	}

	protected virtual Vector3 Accelerate( Vector3 velocity, Vector3 wishDir, float wishSpeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishSpeed > speedLimit )
			wishSpeed = speedLimit;

		var currentSpeed = Velocity.Dot( wishDir );
		var addSpeed = wishSpeed - currentSpeed;

		if ( addSpeed <= 0 )
			return velocity;

		var accelSpeed = acceleration * Time.Delta * wishSpeed * 1f;

		if ( accelSpeed > addSpeed )
			accelSpeed = addSpeed;

		velocity += wishDir * accelSpeed;

		return velocity;
	}

	protected virtual Vector3 GetWishDirection()
	{
		if ( Path == null ) return Vector3.Zero;
		if ( Path.Count == 0 ) return Vector3.Zero;

		var firstSegment = Path.Segments[0];

		if ( firstSegment.SegmentType == NavNodeType.OnGround )
		{
			if ( Position.Distance( firstSegment.Position ) > 80f )
			{
				var direction = (firstSegment.Position - Position).Normal.WithZ( 0f );
				return direction;
			}
		}

		Path.Segments.RemoveAt( 0 );
		return Vector3.Zero;
	}

	[ForsakenEvent.NavBlockerAdded]
	protected virtual void OnNavBlockerAdded( Vector3 position )
	{
		if ( Path == null || Path.Count == 0 )
			return;

		SnapToNavMesh();

		if ( !MoveToLocation( TargetLocation ) )
		{
			NextWanderTime = 0f;
		}
	}
}
