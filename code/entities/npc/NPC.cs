using Facepunch.Forsaken.FlowFields;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public abstract partial class NPC : AnimatedEntity, IMoveAgent
{
	protected virtual bool UseMoveHelper => false;
	protected virtual bool UseGravity => false;

	protected Vector3 TargetLocation { get; set; }
	protected TimeUntil NextWanderTime { get; set; }
	protected Vector3 WishDirection { get; set; }

	public virtual float AgentRadius => 28f;
	public virtual Pathfinder Pathfinder => PathManager.Default;
	public MoveGroup MoveGroup { get; private set; }
	public PathRequest Path { get; private set; }

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

	public virtual void OnMoveGroupDisposed( MoveGroup group )
	{

	}

	public Vector3? GetFreePositionAtTarget( Vector3 target, float radius )
	{
		var pathfinder = Pathfinder;
		var potentialNodes = new List<Vector3>();

		pathfinder.GetGridPositions( target, radius, potentialNodes, true );

		var freeLocations = potentialNodes
			.OrderBy( v => v.Distance( Position ) )
			.ToList();

		if ( freeLocations.Count > 0 )
			return freeLocations[0];
		else
			return null;
	}

	protected bool MoveToLocation( Vector3 position )
	{
		TargetLocation = position;

		var freePosition = GetFreePositionAtTarget( position, 128f );

		if ( freePosition.HasValue )
		{
			var wp = Pathfinder.CreateWorldPosition( freePosition.Value );
			Pathfinder.DrawBox( wp, Color.Blue, 20f );
			DebugOverlay.Line( Position + Vector3.Up * 64f, freePosition.Value, Color.Blue, 20f );
			Path = Pathfinder.Request( freePosition.Value );
		}
		else
		{
			Path = null;
		}

		return Path.IsValid();
	}

	protected bool GetRandomPositionInRange( float range, out Vector3 position )
	{
		var potentialNodes = new List<Vector3>();

		Pathfinder.GetGridPositions( Position, range, potentialNodes );

		if ( potentialNodes.Count > 0 )
		{
			position = Game.Random.FromList( potentialNodes );
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

		if ( ShouldWander() && !Path.IsValid() )
		{
			if ( NextWanderTime && GetRandomPositionInRange( 4000f, out var targetPosition ) )
			{
				MoveToLocation( targetPosition );
				NextWanderTime = GetIdleDuration();
			}
		}

		if ( UseGravity )
		{
			var trace = Trace.Ray( Position + Vector3.Up * 8f, Position + Vector3.Down * 32f )
				.WorldOnly()
				.Ignore( this )
				.Run();

			GroundEntity = trace.Entity;
			Velocity += Vector3.Down * 700f * Time.Delta;
			Velocity = ApplyFriction( Velocity, 4f );
		}
		else
		{
			GroundEntity = Game.WorldEntity;
			Velocity = ApplyFriction( Velocity, 4f );
		}

		var wishDirection = GetWishDirection();
		Velocity = Accelerate( Velocity, wishDirection, GetMoveSpeed(), 0f, 8f );

		if ( wishDirection.Length > 0f )
		{
			var targetRotation = Rotation.LookAt( wishDirection.WithZ( 0f ), Vector3.Up );
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

		if ( Path.IsValid() && Path.IsDestination( Position ) )
		{
			Log.Info( this + " finished path" );
			OnFinishedPath();
			Path = null;
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

	protected virtual void OnFinishedPath()
	{
		NextWanderTime = GetIdleDuration();
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
		if ( !Path.IsValid() )
			return default;

		var offset = Pathfinder.CenterOffset.Normal;
		var direction = Path.GetDirection( Position );

		return (direction.Normal * offset);
	}

	[ForsakenEvent.NavBlockerAdded]
	protected virtual void OnNavBlockerAdded( Vector3 position )
	{
		if ( !Path.IsValid() ) return;

		if ( !MoveToLocation( TargetLocation ) )
		{
			NextWanderTime = 0f;
		}
	}
}
