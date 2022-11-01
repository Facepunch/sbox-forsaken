using Sandbox;
using System;

namespace Facepunch.Forsaken;

public partial class MoveController : BasePlayerController
{
	[Net] public float WalkSpeed { get; set; }
	[Net] public float SprintSpeed { get; set; }

	public float FallDamageThreshold { get; set; } = 250f;
	public float MoveSpeedScale { get; set; } = 1f;
	public float Acceleration { get; set; } = 8f;
	public float AirAcceleration { get; set; } = 24f;
	public float GroundFriction { get; set; } = 6f;
	public float StopSpeed { get; set; } = 100f;
	public float FallDamageMin { get; set; } = 0f;
	public float FallDamageMax { get; set; } = 100f;
	public float StayOnGroundAngle { get; set; } = 270f;
	public float GroundAngle { get; set; } = 46f;
	public float StepSize { get; set; } = 28f;
	public float MaxNonJumpVelocity { get; set; } = 140f;
	public float BodyGirth { get; set; } = 32f;
	public float BodyHeight { get; set; } = 72f;
	public float EyeHeight { get; set; } = 72f;
	public float Gravity { get; set; } = 800f;
	public float AirControl { get; set; } = 48f;

	protected Unstuck Unstuck { get; private set; }

	protected float SurfaceFriction { get; set; }
	protected Vector3 PreVelocity { get; set; }
	protected Vector3 Mins { get; set; }
	protected Vector3 Maxs { get; set; }

	public MoveDuck Duck { get; private set; }

	public bool IsServer => Host.IsServer;
	public bool IsClient => Host.IsClient;

	public MoveController()
	{
		Duck = new MoveDuck( this );
		Unstuck = new Unstuck( this );
	}

	public void ClearGroundEntity()
	{
		if ( GroundEntity == null ) return;

		GroundEntity = null;
		GroundNormal = Vector3.Up;
		SurfaceFriction = 1f;
	}

	public override BBox GetHull()
	{
		var girth = BodyGirth * 0.5f;
		var mins = new Vector3( -girth, -girth, 0 );
		var maxs = new Vector3( +girth, +girth, BodyHeight );
		return new BBox( mins, maxs );
	}

	private void SetBBox( Vector3 mins, Vector3 maxs )
	{
		if ( Mins == mins && Maxs == maxs )
			return;

		Mins = mins;
		Maxs = maxs;
	}

	private void UpdateBBox()
	{
		var girth = BodyGirth * 0.5f;
		var mins = Scale( new Vector3( -girth, -girth, 0 ) );
		var maxs = Scale( new Vector3( +girth, +girth, BodyHeight ) );
		SetBBox( mins, maxs );
	}

	public override void Simulate()
	{
		if ( Pawn is not Player player )
			return;

		EyeLocalPosition = Vector3.Up * Scale( EyeHeight );
		UpdateBBox();

		EyeLocalPosition += TraceOffset;

		if ( Input.Down( InputButton.Run ) )
			EyeRotation = Rotation;
		else
			EyeRotation = player.ViewAngles.ToRotation();

		if ( Unstuck.TestAndFix() )
		{
			// I hope this never really happens.
			return;
		}

		PreVelocity = Velocity;

		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
		Velocity += new Vector3( 0, 0, BaseVelocity.z ) * Time.Delta;
		BaseVelocity = BaseVelocity.WithZ( 0 );

		var startOnGround = GroundEntity != null;

		if ( startOnGround )
		{
			Velocity = Velocity.WithZ( 0 );
			ApplyFriction( GroundFriction * SurfaceFriction );
		}

		WishVelocity = new Vector3( player.InputDirection.x, player.InputDirection.y, 0 );
		var inSpeed = WishVelocity.Length.Clamp( 0, 1 );

		WishVelocity = WishVelocity.WithZ( 0 );
		WishVelocity = WishVelocity.Normal * inSpeed;
		WishVelocity *= GetWishSpeed();

		Duck.PreTick();

		if ( player.IsValid() )
		{
			if ( Input.Down( InputButton.Run ) && !Input.Down( InputButton.Duck ) && WishVelocity.Length > 1f )
				player.ReduceStamina( 10f * Time.Delta );
			else
				player.GainStamina( 15f * Time.Delta );
		}

		var stayOnGround = false;

		if ( GroundEntity != null )
		{
			stayOnGround = true;
			WalkMove();
		}
		else
		{
			AirMove();
		}

		CategorizePosition( stayOnGround );

		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		if ( GroundEntity != null )
		{
			Velocity = Velocity.WithZ( 0 );
		}
	}

	private float GetWishSpeed()
	{
		var wishSpeed = Duck.GetWishSpeed();
		if ( wishSpeed >= 0f ) return wishSpeed;

		var isSprinting = Input.Down( InputButton.Run );

		if ( Client.Pawn is Player player )
		{
			if ( player.IsOutOfBreath )
			{
				isSprinting = false;
			}
		}

		if ( isSprinting )
			return Scale( SprintSpeed * MoveSpeedScale );
		else
			return Scale( WalkSpeed * MoveSpeedScale );
	}

	private void WalkMove()
	{
		var wishDir = WishVelocity.Normal;
		var wishSpeed = WishVelocity.Length;

		WishVelocity = WishVelocity.WithZ( 0 );
		WishVelocity = WishVelocity.Normal * wishSpeed;

		Velocity = Velocity.WithZ( 0 );

		Accelerate( wishDir, wishSpeed, 0f, Acceleration );

		Velocity = Velocity.WithZ( 0 );
		Velocity += BaseVelocity;

		try
		{
			if ( Velocity.Length < 1f )
			{
				Velocity = Vector3.Zero;
				return;
			}

			var dest = (Position + Velocity * Time.Delta).WithZ( Position.z );
			var pm = TraceBBox( Position, dest );

			if ( pm.Fraction == 1 )
			{
				Position = pm.EndPosition;
				StayOnGround();
				return;
			}

			StepMove();
		}
		finally
		{
			Velocity -= BaseVelocity;
		}

		StayOnGround();
	}

	private void StepMove()
	{
		var mover = new MoveHelper( Position, Velocity );
		mover.Trace = mover.Trace.Size( Mins, Maxs ).Ignore( Pawn );
		mover.MaxStandableAngle = GroundAngle;
		mover.TryMoveWithStep( Time.Delta, StepSize );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	private void Move()
	{
		var mover = new MoveHelper( Position, Velocity );
		mover.Trace = mover.Trace.Size( Mins, Maxs ).Ignore( Pawn );
		mover.MaxStandableAngle = GroundAngle;
		mover.TryMove( Time.Delta );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	private void Accelerate( Vector3 wishDir, float wishSpeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishSpeed > speedLimit )
			wishSpeed = speedLimit;

		var currentSpeed = Velocity.Dot( wishDir );
		var addSpeed = wishSpeed - currentSpeed;

		if ( addSpeed <= 0 )
			return;

		var accelSpeed = acceleration * Time.Delta * wishSpeed * SurfaceFriction;

		if ( accelSpeed > addSpeed )
			accelSpeed = addSpeed;

		Velocity += wishDir * accelSpeed;
	}

	private void ApplyFriction( float frictionAmount = 1f )
	{
		var speed = Velocity.Length;
		if ( speed < 0.1f ) return;

		var control = (speed < StopSpeed) ? StopSpeed : speed;
		var drop = control * Time.Delta * frictionAmount;
		var newSpeed = speed - drop;

		if ( newSpeed < 0 ) newSpeed = 0;

		if ( newSpeed != speed )
		{
			newSpeed /= speed;
			Velocity *= newSpeed;
		}
	}

	private float Scale( float speed )
	{
		return speed * Pawn.Scale;
	}

	private Vector3 Scale( Vector3 velocity )
	{
		return velocity * Pawn.Scale;
	}

	private void AirMove()
	{
		var wishDir = WishVelocity.Normal;
		var wishSpeed = WishVelocity.Length;

		Accelerate( wishDir, wishSpeed, AirControl, AirAcceleration );

		Velocity += BaseVelocity;

		Move();

		Velocity -= BaseVelocity;
	}

	private void WaterMove()
	{
		var wishDir = WishVelocity.Normal;
		var wishSpeed = WishVelocity.Length;

		wishSpeed *= 0.8f;

		Accelerate( wishDir, wishSpeed, 100f, Acceleration );

		Velocity += BaseVelocity;

		Move();

		Velocity -= BaseVelocity;
	}

	private void CategorizePosition( bool stayOnGround )
	{
		SurfaceFriction = 1f;

		var point = Position - Vector3.Up * 2f;
		var bumpOrigin = Position;
		var moveToEndPos = false;

		if ( GroundEntity != null )
		{
			moveToEndPos = true;
			point.z -= StepSize;
		}
		else if ( stayOnGround )
		{
			moveToEndPos = true;
			point.z -= StepSize;
		}

		if ( Velocity.z > MaxNonJumpVelocity )
		{
			ClearGroundEntity();
			return;
		}

		var pm = TraceBBox( bumpOrigin, point, 16f );

		if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) > StayOnGroundAngle )
		{
			ClearGroundEntity();
			moveToEndPos = false;

			if ( Velocity.z > 0 )
				SurfaceFriction = 0.25f;
		}
		else
		{
			UpdateGroundEntity( pm );
		}

		if ( moveToEndPos && !pm.StartedSolid && pm.Fraction > 0f && pm.Fraction < 1f )
		{
			Position = pm.EndPosition;
		}
	}

	private void UpdateGroundEntity( TraceResult trace )
	{
		var wasOnGround = (GroundEntity != null);

		GroundNormal = trace.Normal;
		GroundEntity = trace.Entity;
		SurfaceFriction = trace.Surface.Friction * 1.25f;

		if ( SurfaceFriction > 1f )
			SurfaceFriction = 1f;

		if ( GroundEntity != null )
		{
			BaseVelocity = GroundEntity.Velocity;
		}
	}

	public override TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0f )
	{
		return TraceBBox( start, end, Mins, Maxs, liftFeet );
	}

	private void StayOnGround()
	{
		var start = Position + Vector3.Up * 2;
		var end = Position + Vector3.Down * StepSize;

		var trace = TraceBBox( Position, start );
		start = trace.EndPosition;

		trace = TraceBBox( start, end );

		if ( trace.Fraction <= 0 ) return;
		if ( trace.Fraction >= 1 ) return;
		if ( trace.StartedSolid ) return;
		if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > StayOnGroundAngle ) return;

		Position = trace.EndPosition;
	}
}
