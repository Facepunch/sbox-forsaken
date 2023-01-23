using Sandbox;

namespace Facepunch.Forsaken;

public partial class NPC : AnimatedEntity
{
	/// <summary>
	/// The display name of the NPC.
	/// </summary>
	[Net, Property] public string DisplayName { get; set; } = "NPC";

	/// <summary>
	/// Whether or not the NPC randomly wanders around the map.
	/// </summary>
	[Property] public bool DoesWander { get; set; } = false;

	/// <summary>
	/// The minumum amount of time that the NPC will stay idle for before wandering again.
	/// </summary>
	[Property] public float MinIdleDuration { get; set; } = 30f;

	/// <summary>
	/// The maximum amount of time that the NPC will stay idle for before wandering again.
	/// </summary>
	[Property] public float MaxIdleDuration { get; set; } = 60f;

	/// <summary>
	/// The speed at which the NPC moves.
	/// </summary>
	public virtual float MoveSpeed { get; set; } = 80f;

	protected TimeUntil NextWanderTime { get; set; }
	protected Vector3 WishDirection { get; set; }
	protected NavPath Path { get; set; }

	public override void Spawn()
	{
		Tags.Add( "npc" );

		base.Spawn();
	}

	[Event.Tick.Server]
	protected virtual void ServerTick()
	{
		if ( DoesWander && NextWanderTime && NavMesh.IsLoaded )
		{
			NextWanderTime = Game.Random.Float( MinIdleDuration, MaxIdleDuration );

			var targetPosition = NavMesh.GetPointWithinRadius( Position, 500f, 5000f );
			if ( !targetPosition.HasValue ) return;

			Path = NavMesh.PathBuilder( Position )
				.WithMaxClimbDistance( 8f )
				.WithMaxDropDistance( 8f )
				.WithStepHeight( 24f )
				.Build( targetPosition.Value );
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
		Velocity = Accelerate( Velocity, wishDirection, MoveSpeed, 0f, 8f );

		if ( wishDirection.Length > 0f )
		{
			var targetRotation = Rotation.LookAt( wishDirection, Vector3.Up );
			Rotation = Rotation.Lerp( Rotation, targetRotation, Time.Delta * 10f );
		}

		HandleAnimation();

		var mover = new MoveHelper( Position, Velocity );

		mover.Trace = mover.SetupTrace()
			.WithoutTags( "passplayers", "player" )
			.WithAnyTags( "solid", "playerclip", "passbullets" )
			.Size( GetHull() )
			.Ignore( this );

		mover.MaxStandableAngle = 46f;
		mover.TryMoveWithStep( Time.Delta, 28f );

		Position = mover.Position;
		Velocity = mover.Velocity;
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
		var animHelper = new CitizenAnimationHelper( this );

		animHelper.WithWishVelocity( Velocity );
		animHelper.WithVelocity( Velocity );
		animHelper.WithLookAt( Position + Rotation.Forward * 100f, 1f, 1f, 0.5f );
		animHelper.AimAngle = Rotation;
		animHelper.DuckLevel = 0f;
		animHelper.VoiceLevel = 0f;
		animHelper.IsGrounded = GroundEntity.IsValid();
		animHelper.IsSitting = false;
		animHelper.IsNoclipping = false;
		animHelper.IsClimbing = false;
		animHelper.IsSwimming = false;
		animHelper.IsWeaponLowered = false;
		animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
		animHelper.AimBodyWeight = 0.5f;
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
}
