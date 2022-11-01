using System;
using Sandbox;

namespace Facepunch.Forsaken;

public partial class Player : Sandbox.Player
{
	[Net, Predicted] public float Stamina { get; private set; }
	[Net, Predicted] public bool IsOutOfBreath { get; private set; }

	[ClientInput] public Vector3 CursorDirection { get; private set; }
	[ClientInput] public Vector3 CameraPosition { get; private set; }

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
		CameraPosition = CurrentView.Position;

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

	[ConVar.ClientData] public string StructurePiece { get; set; }
	public Structure GhostStructure { get; set; }
	private string CurrentPiece { get; set; }

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		if ( Stamina <= 10f )
			IsOutOfBreath = true;
		else if ( IsOutOfBreath && Stamina >= 25f )
			IsOutOfBreath = false;

		var pieceName = cl.GetClientData( "StructurePiece" );

		if ( !string.IsNullOrEmpty( pieceName ) )
		{
			var trace = Trace.Ray( CameraPosition, CameraPosition + CursorDirection * 1000f )
				.WorldOnly()
				.Run();

			if ( trace.Hit && trace.Normal.Dot( Vector3.Up ) == 1f )
			{
				if ( IsClient )
				{
					if ( !GhostStructure.IsValid() || CurrentPiece != pieceName )
					{
						GhostStructure?.Delete();

						if ( pieceName == "wall" )
							GhostStructure = new Wall();
						else if ( pieceName == "foundation" )
							GhostStructure = new Foundation();
						else if ( pieceName == "doorway" )
							GhostStructure = new Doorway();

						CurrentPiece = pieceName;
					}
				}

				if ( IsClient && GhostStructure.IsValid() )
				{
					GhostStructure.Position = trace.EndPosition;
				}

				if ( Prediction.FirstTime && Input.Released( InputButton.PrimaryAttack ) )
				{
					if ( IsServer )
					{
						Structure structure = null;

						if ( pieceName == "wall" )
							structure = new Wall();
						else if ( pieceName == "foundation" )
							structure = new Foundation();
						else if ( pieceName == "doorway" )
							structure = new Doorway();

						if ( structure.IsValid() )
						{
							structure.Position = trace.EndPosition;
						}
					}
					else
					{
						GhostStructure.Delete();
					}
				}
			}
		}
	}
}
