﻿using System;
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

	private TypeDescription LastStructureType { get; set; }
	public Structure GhostStructure { get; set; }

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
		var selectedStructureType = TypeLibrary.GetDescriptionByIdent( StructureType );
		if ( selectedStructureType == null ) return;

		var trace = Trace.Ray( CameraPosition, CameraPosition + CursorDirection * 1000f )
			.WorldOnly()
			.Run();

		if ( trace.Hit && trace.Normal.Dot( Vector3.Up ) == 1f )
		{
			if ( IsClient )
			{
				if ( !GhostStructure.IsValid() || LastStructureType != selectedStructureType )
				{
					GhostStructure?.Delete();
					GhostStructure = selectedStructureType.Create<Structure>();
					LastStructureType = selectedStructureType;
				}
			}

			if ( IsClient && GhostStructure.IsValid() )
			{
				GhostStructure.EnableShadowCasting = false;
				GhostStructure.EnableShadowReceive = false;
				GhostStructure.RenderColor = Color.Green.WithAlpha( 0.8f );

				if ( GhostStructure.LocateSocket( trace.EndPosition, out var socket ) )
				{
					var transform = socket.Transform;
					GhostStructure.Position = transform.Position;
					GhostStructure.Rotation = transform.Rotation;
					GhostStructure.ResetInterpolation();

					DebugOverlay.Sphere( transform.Position, 16f, Color.Cyan, Time.Delta * 4f );
				}
				else
				{
					GhostStructure.Position = trace.EndPosition;
					GhostStructure.ResetInterpolation();

					if ( GhostStructure.RequiresSocket )
						GhostStructure.RenderColor = Color.Red.WithAlpha( 0.3f );
				}
			}

			if ( Prediction.FirstTime && Input.Released( InputButton.PrimaryAttack ) )
			{
				if ( IsServer )
				{
					var structure = selectedStructureType.Create<Structure>();

					if ( structure.IsValid() )
					{
						if ( structure.LocateSocket( trace.EndPosition, out var socket ) )
						{
							var transform = socket.Transform;
							structure.Position = transform.Position;
							structure.Rotation = transform.Rotation;
							structure.ResetInterpolation();
							socket.Add( structure );
						}
						else if ( !structure.RequiresSocket )
						{
							structure.Position = trace.EndPosition;
						}
						else
						{
							structure.Delete();
						}
					}
				}

				GhostStructure?.Delete();
			}
		}
	}
}