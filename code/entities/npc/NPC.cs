using Sandbox;

namespace Facepunch.Forsaken;

public abstract partial class NPC : AnimatedEntity
{
	protected Vector3 TargetLocation { get; set; }
	protected Vector3 WishDirection { get; set; }
	protected NavPath Path { get; set; }

	private GravityComponent Gravity { get; set; }
	private FrictionComponent Friction { get; set; }

	public override void Spawn()
	{
		Tags.Add( "npc" );

		Gravity = Components.GetOrCreate<GravityComponent>();
		Friction = Components.GetOrCreate<FrictionComponent>();

		base.Spawn();
	}

	public bool HasValidPath()
	{
		if ( Path is null ) return false;
		if ( Path.Count == 0 ) return false;
		return true;
	}

	public virtual string GetDisplayName()
	{
		return "NPC";
	}

	public virtual float GetMoveSpeed()
	{
		return 80f;
	}

	protected void SnapToNavMesh()
	{
		var closest = NavMesh.GetClosestPoint( Position );

		if ( closest.HasValue )
			Position = closest.Value;
	}

	protected void RotateOverTime( Vector3 direction )
	{
		var targetRotation = Rotation.LookAt( direction.WithZ( 0f ), Vector3.Up );
		Rotation = Rotation.Lerp( Rotation, targetRotation, Time.Delta * 10f );
	}

	protected void RotateOverTime( Entity target )
	{
		var direction = (target.Position - Position).Normal;
		var targetRotation = Rotation.LookAt( direction.WithZ( 0f ), Vector3.Up );
		Rotation = Rotation.Lerp( Rotation, targetRotation, Time.Delta * 10f );
	}

	protected bool MoveToLocation( Vector3 position, float stepSize = 24f )
	{
		var closestPoint = NavMesh.GetClosestPoint( position );
		if ( !closestPoint.HasValue ) return false;

		TargetLocation = closestPoint.Value;

		Path = NavMesh.PathBuilder( Position )
			.WithMaxClimbDistance( 0f )
			.WithMaxDropDistance( 128f )
			.WithAgentHull( NavAgentHull.Any )
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

		HandleBehavior();

		var wishDirection = GetWishDirection();

		UpdateVelocity( wishDirection );
		UpdateRotation( wishDirection );

		Gravity.Update();
		Friction.Update();

		HandleAnimation();

		Position += Velocity * Time.Delta;

		/*
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
		*/
	}

	protected virtual void UpdateRotation( Vector3 direction )
	{

	}

	protected virtual void UpdateVelocity( Vector3 direction )
	{

	}

	protected virtual void HandleBehavior()
	{

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
		var girth = 12f;
		var mins = new Vector3( -girth, -girth, 0f );
		var maxs = new Vector3( +girth, +girth, 72f );
		return new BBox( mins, maxs );
	}

	protected virtual void OnFinishedPath()
	{

	}

	protected virtual Vector3 GetWishDirection()
	{
		if ( !HasValidPath() )
			return Vector3.Zero;

		var firstSegment = Path.Segments[0];

		if ( Position.Distance( firstSegment.Position ) > 10f )
		{
			var direction = (firstSegment.Position - Position).Normal;
			return direction;
		}

		Path.Segments.RemoveAt( 0 );

		if ( Path.Segments.Count == 0 )
		{
			OnFinishedPath();
		}

		return Vector3.Zero;
	}

	[ForsakenEvent.NavBlockerAdded]
	protected virtual void OnNavBlockerAdded( Vector3 position )
	{
		if ( !HasValidPath() ) return;

		SnapToNavMesh();
		MoveToLocation( TargetLocation );
	}
}
