using System;
using System.Linq;
using Sandbox;

namespace Facepunch.Forsaken;

public partial class Player : Sandbox.Player
{
	[Net, Predicted] public float Stamina { get; private set; }
	[Net, Predicted] public bool IsOutOfBreath { get; private set; }

	[ClientInput] public Vector3 CursorDirection { get; private set; }
	[ClientInput] public Vector3 CameraPosition { get; private set; }

	[Net] public int StructureType { get; private set; }

	[ConCmd.Server( "fsk.structure.selected" )]
	private static void SetSelectedStructureCmd( int identity )
	{
		if ( ConsoleSystem.Caller.Pawn is Player player )
		{
			player.StructureType = identity;
		}
	}

	public void SetStructureType( TypeDescription type )
	{
		Host.AssertClient();
		Assert.NotNull( type );
		SetSelectedStructureCmd( type.Identity );
	}

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

		if ( Mouse.Visible )
			CursorDirection = Screen.GetDirection( Mouse.Position );

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

	public override void Simulate( Client client )
	{
		base.Simulate( client );

		if ( Stamina <= 10f )
			IsOutOfBreath = true;
		else if ( IsOutOfBreath && Stamina >= 25f )
			IsOutOfBreath = false;

		DoStructurePlacement( client );
	}

	private void DoStructurePlacement( Client client )
	{
		var structureType = TypeLibrary.GetDescriptionByIdent( StructureType );
		if ( structureType == null ) return;

		var trace = Trace.Ray( CameraPosition, CameraPosition + CursorDirection * 1000f )
			.WorldOnly()
			.Run();

		if ( trace.Hit )
		{
			if ( IsClient )
			{
				var ghost = Structure.GetOrCreateGhost( structureType );

				ghost.RenderColor = Color.Cyan.WithAlpha( 0.5f );

				var match = ghost.LocateSocket( trace.EndPosition );

				if ( match.IsValid )
				{
					ghost.SnapToSocket( match );
				}
				else
				{
					ghost.Position = trace.EndPosition;
					ghost.ResetInterpolation();

					if ( ghost.RequiresSocket || !ghost.IsValidPlacement( ghost.Position, trace.Normal ) )
						ghost.RenderColor = Color.Red.WithAlpha( 0.5f );
				}
			}

			if ( Prediction.FirstTime && Input.Released( InputButton.PrimaryAttack ) )
			{
				if ( IsServer )
				{
					var structure = structureType.Create<Structure>();

					if ( structure.IsValid() )
					{
						var isValid = false;
						var match = structure.LocateSocket( trace.EndPosition );

						if ( match.IsValid )
						{
							structure.SnapToSocket( match );
							structure.OnConnected( match.Ours, match.Theirs );
							match.Ours.Connect( match.Theirs );
							isValid = true;
						}
						else if ( !structure.RequiresSocket )
						{
							structure.Position = trace.EndPosition;
							isValid = structure.IsValidPlacement( structure.Position, trace.Normal );
						}

						if ( !isValid )
						{
							structure.Delete();
						}
					}
				}

				Structure.ClearGhost();
			}
		}
	}
}
