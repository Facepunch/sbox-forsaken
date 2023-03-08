using Sandbox;

namespace Facepunch.Forsaken;

public abstract partial class Animal : NPC
{
	protected enum MovementState
	{
		Idle,
		Moving
	}

	protected WanderBehavior Wander { get; set; }
	protected AvoidanceBehavior Avoidance { get; set; }
	protected SteeringComponent Steering { get; set; }

	protected TimeUntil NextChangeState { get; set; }
	protected TimeUntil NextWanderTime { get; set; }
	protected MovementState State { get; set; }

	public override void Spawn()
	{
		Steering = Components.GetOrCreate<SteeringComponent>();
		Avoidance = Components.GetOrCreate<AvoidanceBehavior>();
		Wander = Components.GetOrCreate<WanderBehavior>();

		NextChangeState = Game.Random.Float( 4f, 8f );
		State = MovementState.Idle;

		base.Spawn();
	}

	protected override void HandleBehavior()
	{
		Steering.MaxVelocity = GetMoveSpeed();
		Steering.MaxAcceleration = Steering.MaxVelocity * 0.25f;

		if ( NextWanderTime )
		{
			NextWanderTime = Game.Random.Float( 4f, 8f );
			Wander.Regenerate();
		}

		if ( NextChangeState )
		{
			if ( State == MovementState.Idle )
			{
				NextChangeState = Game.Random.Float( 10f, 20f );
				State = MovementState.Moving;
			}
			else
			{
				NextChangeState = Game.Random.Float( 8f, 16f );
				State = MovementState.Idle;
			}
		}

		base.HandleBehavior();
	}

	protected override void UpdateRotation()
	{
		Steering.RotateToTarget();
	}

	protected override void UpdateVelocity()
	{
		if ( State == MovementState.Idle )
		{
			Velocity = Vector3.Zero;
			return;
		}

		var acceleration = Avoidance.GetSteering();
		acceleration += Wander.GetSteering();

		if ( !acceleration.IsNearZeroLength )
		{
			Steering.Steer( acceleration );
		}
	}

	protected override void HandleAnimation()
	{
		
	}
}
