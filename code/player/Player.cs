using System;
using Sandbox;

namespace Facepunch.Forsaken;

public partial class Player : Sandbox.Player
{
	[Net, Predicted] public float Stamina { get; private set; }
	[Net, Predicted] public bool IsOutOfBreath { get; private set; }

	[ClientInput] public Vector3 CursorDirection { get; private set; }

	public void ReduceStamina( float amount )
	{
		Stamina = Math.Max( Stamina - amount, 0f );
	}

	public void GainStamina( float amount )
	{
		Stamina = Math.Min( Stamina + amount, 100f );
	}

	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new MoveController
		{
			SprintSpeed = 200f,
			WalkSpeed = 100f
		};

		CameraMode = new TopDownCamera();
		Animator = new PlayerAnimator();

		base.Spawn();
	}

	public override void BuildInput()
	{
		base.BuildInput();

		CursorDirection = Mouse.Visible ? Screen.GetDirection( Mouse.Position ) : CurrentView.Rotation.Forward;

		var tablePlane = new Plane( Position, Vector3.Up );
		var hitPosition = tablePlane.Trace( new Ray( EyePosition, CursorDirection ), true );

		if ( hitPosition.HasValue )
		{
			var direction = (hitPosition.Value - Position).Normal;
			ViewAngles = direction.EulerAngles;
		}
	}

	public override void Respawn()
	{
		Stamina = 100f;

		base.Respawn();
	}

	public override void Simulate( Client cl )
	{
		if ( Stamina <= 10f )
			IsOutOfBreath = true;
		else if ( IsOutOfBreath && Stamina >= 25f )
			IsOutOfBreath = false;

		base.Simulate( cl );
	}
}
