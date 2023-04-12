using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class NPC : AnimatedEntity
{
	[ConVar.Server( "fsk.npc.debug" )]
	public static bool Debug { get; set; } = false;

	[Property]
	[Description( "The path that this NPC should try to patrol." )]
	public string PatrolPathTarget { get; set; }

	protected GenericPathEntity PatrolPathEntity => FindByName( PatrolPathTarget ) as GenericPathEntity;
	protected Vector3[] PathPoints { get; set; } = new Vector3[64];
	protected bool IsPatrolling { get; private set; }
	protected List<Vector3> Path { get; set; }

	private GravityComponent Gravity { get; set; }
	private FrictionComponent Friction { get; set; }
	private TimeUntil NextCheckSimulate { get; set; }
	private TimeSince TimeSinceLastFootstep { get; set; }
	private bool ReversePatrolDirection { get; set; }
	private bool ShouldSimulate { get; set; }

	public override void Spawn()
	{
		Tags.Add( "npc" );

		Gravity = Components.GetOrCreate<GravityComponent>();
		Friction = Components.GetOrCreate<FrictionComponent>();

		base.Spawn();
	}

	public void ClearPath()
	{
		Path?.Clear();
		Path = null;
	}

	public void FindPatrolPath( bool snapToPath )
	{
		var pathEntity = PatrolPathEntity;

		if ( !pathEntity.IsValid() )
			return;

		BasePathNode closestNode = null;
		var smallestDistance = 0f;
		var index = 0;

		for ( int i = 0; i < pathEntity.PathNodes.Count; i++ )
		{
			BasePathNode node = pathEntity.PathNodes[i];
			var distance = Position.Distance( node.WorldPosition );
			if ( closestNode is null || distance < smallestDistance )
			{
				smallestDistance = distance;
				closestNode = node;
				index = i;
			}
		}

		Path ??= new();
		Path.Clear();

		if ( ReversePatrolDirection )
		{
			for ( var i = index; i >= 0; i-- )
			{
				var position = pathEntity.PathNodes[i].WorldPosition;
				Path.Add( position );
			}
		}
		else
		{
			for ( var i = index; i < pathEntity.PathNodes.Count; i++ )
			{
				var position = pathEntity.PathNodes[i].WorldPosition;
				Path.Add( position );
			}
		}

		if ( snapToPath )
		{
			Position = pathEntity.PathNodes[index].WorldPosition;
		}

		IsPatrolling = true;
	}

	public bool TryFindPath( Vector3 position, bool requireFullPath = false )
	{
		var p = Navigation.CalculatePath( Position, position, PathPoints, requireFullPath );

		IsPatrolling = false;

		if ( p > 0 )
		{
			Path ??= new();
			Path.Clear();

			for ( var i = 0; i < p; i++ )
			{
				Path.Add( Navigation.WithZOffset( PathPoints[i] ) );
			}

			return true;
		}
		else
		{
			Path?.Clear();
			return false;
		}
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

	public override void OnAnimEventFootstep( Vector3 position, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( !Game.IsClient )
			return;

		if ( TimeSinceLastFootstep < 0.2f )
			return;

		volume *= 1f;

		TimeSinceLastFootstep = 0f;

		var tr = Trace.Ray( position, position + Vector3.Down * 20f )
			.WithoutTags( "trigger" )
			.Radius( 1f )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	protected bool CheckShouldSimulate()
	{
		if ( NextCheckSimulate )
		{
			ShouldSimulate = FindInSphere( Position, 2048f )
				.OfType<ForsakenPlayer>()
				.Any();

			NextCheckSimulate = 1f;
		}

		return ShouldSimulate;
	}

	protected Vector3 GetPathTarget()
	{
		if ( !HasValidPath() )
			return Vector3.Zero;

		return Path.First();
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

	protected void UpdatePath()
	{
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
			OnFinishedPath();
		}
	}

	[Event.Tick.Server]
	protected void ServerTick()
	{
		if ( CheckShouldSimulate() )
		{
			UpdateLogic();
		}
	}

	[Event.Entity.PostSpawn]
	protected virtual void OnMapLoaded()
	{
		if ( PatrolPathEntity.IsValid() )
		{
			FindPatrolPath( true );
		}
	}

	protected virtual void UpdateLogic()
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
		if ( IsPatrolling )
		{
			ReversePatrolDirection = !ReversePatrolDirection;
			FindPatrolPath( false );
		}
	}
}
