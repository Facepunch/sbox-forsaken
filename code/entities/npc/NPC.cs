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
	protected NavPath Path { get; set; }

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
			Velocity += Vector3.Down * 600f * Time.Delta;
		}
		else
		{
			Position = Position.WithZ( pm.EndPosition.z );
			Velocity = Velocity.WithZ( 0f );
		}

		ApplyFriction( 4f );
		UpdatePathVelocity();
		HandleAnimation();

		var mover = new MoveHelper( Position, Velocity );
		mover.Trace = mover.Trace.Size( GetHull() ).Ignore( this );
		mover.MaxStandableAngle = 46f;
		mover.TryMoveWithStep( Time.Delta, 28f );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	protected virtual void ApplyFriction( float amount = 1f )
	{
		var speed = Velocity.Length;
		if ( speed < 0.1f ) return;

		var control = (speed < 100f) ? 100f : speed;
		var newSpeed = speed - (control * Time.Delta * amount);

		if ( newSpeed < 0 )
			newSpeed = 0;

		if ( newSpeed != speed )
		{
			newSpeed /= speed;
			Velocity *= newSpeed;
		}
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
			.WithoutTags( "trigger" )
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

	protected virtual void Accelerate( Vector3 wishDir, float wishSpeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishSpeed > speedLimit )
			wishSpeed = speedLimit;

		var currentSpeed = Velocity.Dot( wishDir );
		var addSpeed = wishSpeed - currentSpeed;

		if ( addSpeed <= 0 )
			return;

		var accelSpeed = acceleration * Time.Delta * wishSpeed * 1f;

		if ( accelSpeed > addSpeed )
			accelSpeed = addSpeed;

		Velocity += wishDir * accelSpeed;
	}

	protected virtual void UpdatePathVelocity()
	{
		if ( Path == null ) return;
		if ( Path.Count == 0 ) return;

		var firstSegment = Path.Segments[0];

		if ( firstSegment.SegmentType == NavNodeType.OnGround )
		{
			if ( Position.Distance( firstSegment.Position ) > 80f )
			{
				var direction = (firstSegment.Position - Position).Normal.WithZ( 0f );
				Accelerate( direction, MoveSpeed, 0f, 8f );

				var targetRotation = Rotation.LookAt( direction, Vector3.Up );
				Rotation = Rotation.Lerp( Rotation, targetRotation, Time.Delta * 10f );

				return;
			}
		}

		Path.Segments.RemoveAt( 0 );
	}
}
