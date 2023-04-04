using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class NPC : AnimatedEntity
{
	[ConVar.Server( "fsk.npc.debug" )]
	public static bool Debug { get; set; } = false;

	protected Vector3? TargetLocation { get; set; }
	protected List<Vector3> Path { get; set; }

	private GravityComponent Gravity { get; set; }
	private FrictionComponent Friction { get; set; }
	private TimeUntil NextFindPath { get; set; }
	private TimeUntil NextTrimPath { get; set; }

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

	protected Vector3 GetPathTarget()
	{
		if ( !HasValidPath() )
			return Vector3.Zero;

		return Path.First();
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

	private Vector3[] PathPoints { get; set; } = new Vector3[32];

	protected bool MoveToLocationNavMesh( Vector3 position, float stepSize = 24f )
	{
		var closestPoint = NavMesh.GetClosestPoint( position );
		if ( !closestPoint.HasValue ) return false;

		TargetLocation = closestPoint;

		var path = NavMesh.PathBuilder( Position )
			.WithMaxClimbDistance( 0f )
			.WithMaxDropDistance( 128f )
			.WithAgentHull( NavAgentHull.Default )
			.WithMaxDetourDistance( 64f )
			.WithStepHeight( stepSize )
			.Build( TargetLocation.Value );

		CreateOptimizedPath( path );

		return (Path?.Count ?? 0) > 0;
	}

	protected bool MoveToLocation( Vector3 position )
	{
		if ( Position.Distance( position ) <= 32f )
		{
			// We're already pretty much there.
			return false;
		}

		TargetLocation = position;

		Path ??= new();
		Path.Clear();

		return true;
	}

	protected void CreateOptimizedPath( NavPath path )
	{
		if ( path == null || path.Count == 0 )
			return;

		Path ??= new();
		Path.Clear();

		foreach ( var p in path.Segments )
		{
			Path.Add( p.Position );
		}

		var stepHeight = Vector3.Up * 8f;
		var radius = 4f;

		for ( var i = Path.Count - 1; i >= 1; i-- )
		{
			for ( var j = 1; j < Path.Count; j++ )
			{
				if ( j >= Path.Count ) continue;
				if ( i >= Path.Count ) continue;

				var trace = Trace.Ray( Path[i] + stepHeight, Path[j] + stepHeight )
					.WorldAndEntities()
					.WithoutTags( "trigger", "passplayers" )
					.WithAnyTags( "solid" )
					.Ignore( this )
					.Size( radius )
					.Run();

				if ( !trace.Hit )
				{
					Path.RemoveRange( j, i - j );
					break;
				}
			}
		}
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

	protected void TrimPath()
	{
		if ( !HasValidPath() ) return;

		var stepHeight = Vector3.Up * 8f;
		var radius = 4f;

		for ( var i = Path.Count - 1; i >= 0; i-- )
		{
			var trace = Trace.Ray( Position + stepHeight, Path[i] + stepHeight )
				.WorldAndEntities()
				.WithoutTags( "trigger", "passplayers" )
				.WithAnyTags( "solid" )
				.Ignore( this )
				.Size( radius )
				.Run();

			if ( !trace.Hit )
			{
				Path.RemoveRange( 0, i );

				if ( Path.Count == 0 )
					Path = null;

				return;
			}
		}
	}

	protected void UpdatePath()
	{
		if ( TargetLocation.HasValue && !HasValidPath() && NextFindPath )
		{
			NextFindPath = 1f;

			var p = Navigation.CalculatePath( Position, TargetLocation.Value, PathPoints, true );

			if ( p > 0 )
			{
				Path ??= new();
				Path.Clear();

				for ( var i = 0; i < p; i++ )
				{
					Path.Add( Navigation.WithZOffset( PathPoints[i] ) );
				}
			}
			else
			{
				TargetLocation = null;
				Path?.Clear();
			}
		}

		if ( !HasValidPath() ) return;

		var position = Path[0];

		if ( Debug )
		{
			for ( var i = 0; i < Path.Count; i++ )
			{
				var a = Path[i];

				DebugOverlay.Sphere( a, 32f, Color.Orange );

				if ( Path.Count > i + 1 )
				{
					var b = Path[i + 1];
					DebugOverlay.Line( a, b, Color.Orange );
				}
			}
		}

		if ( Position.Distance( position ) > 10f )
			return;

		Path.RemoveAt( 0 );

		if ( Path.Count == 0 )
		{
			TargetLocation = null;
			OnFinishedPath();
		}
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

		UpdatePath();
		HandleBehavior();

		Gravity.Update();
		Friction.Update();

		UpdateVelocity();
		UpdateRotation();

		HandleAnimation();

		var mover = new MoveHelper( Position, Velocity );

		mover.Trace = mover.SetupTrace()
			.WithoutTags( "passplayers", "player", "npc" )
			.WithAnyTags( "solid", "playerclip", "passbullets" )
			.Size( GetHull() )
			.Ignore( this );

		mover.MaxStandableAngle = 20f;

		if ( mover.TryUnstuck() )
		{
			mover.TryMoveWithStep( Time.Delta, 24f );
		}

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	protected virtual void UpdateRotation()
	{

	}

	protected virtual void UpdateVelocity()
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

	[ForsakenEvent.NavBlockerAdded]
	protected virtual void OnNavBlockerAdded( Vector3 position )
	{
		if ( !HasValidPath() ) return;

		SnapToNavMesh();

		if ( TargetLocation.HasValue )
			MoveToLocation( TargetLocation.Value );
	}
}
