﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Component;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer : Player
{
	public static ForsakenPlayer Me => Local.Pawn as ForsakenPlayer;

	[Net, Predicted] public float Stamina { get; private set; }
	[Net, Predicted] public bool IsOutOfBreath { get; private set; }
	[Net, Predicted] public ushort HotbarIndex { get; private set; }

	[Net] private NetInventoryContainer InternalBackpack { get; set; }
	public InventoryContainer Backpack => InternalBackpack.Value;

	[Net] private NetInventoryContainer InternalHotbar { get; set; }
	public InventoryContainer Hotbar => InternalHotbar.Value;

	[Net] private NetInventoryContainer InternalEquipment { get; set; }
	public InventoryContainer Equipment => InternalEquipment.Value;

	[ClientInput] public Vector3 CursorDirection { get; private set; }
	[ClientInput] public Vector3 CameraPosition { get; private set; }
	[ClientInput] public Entity HoveredEntity { get; private set; }

	public Vector2 Cursor { get; set; }

	public Dictionary<ArmorSlot, List<BaseClothing>> Armor { get; private set; }

	[Net] public int StructureType { get; private set; }

	public ProjectileSimulator Projectiles { get; private set; }
	public DamageInfo LastDamageTaken { get; private set; }

	private TimeSince TimeSinceBackpackOpen { get; set; }
	private bool IsBackpackToggleMode { get; set; }
	private Entity LastHoveredEntity { get; set; }

	[ConCmd.Server( "fsk.structure.selected" )]
	private static void SetSelectedStructureCmd( int identity )
	{
		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer player )
		{
			player.StructureType = identity;
		}
	}

	public ForsakenPlayer() : base()
	{
		Projectiles = new( this );
	}

	public ForsakenPlayer( Client client ) : this()
	{
		HotbarIndex = 0;
		client.Pawn = this;
		CreateInventories();
		Armor = new();
	}

	public void SetStructureType( TypeDescription type )
	{
		Host.AssertClient();
		Assert.NotNull( type );
		SetSelectedStructureCmd( type.Identity );
	}

	[ClientRpc]
	public void ResetCursor()
	{
		Cursor = new Vector2( 0.5f, 0.5f );
	}

	public void ReduceStamina( float amount )
	{
		Stamina = Math.Max( Stamina - amount, 0f );
	}

	public InventoryItem GetActiveHotbarItem()
	{
		return Hotbar.GetFromSlot( HotbarIndex );
	}

	public void GainStamina( float amount )
	{
		Stamina = Math.Min( Stamina + amount, 100f );
	}

	public void RenderHud()
	{ 

	}

	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		base.Spawn();
	}

	public override void BuildInput()
	{
		base.BuildInput();

		if ( Input.StopProcessing )
		{
			HoveredEntity = null;
			return;
		}

		var mouseDelta = Input.MouseDelta / new Vector2( Screen.Width, Screen.Height );
		var sensitivity = 0.06f;

		if ( !Mouse.Visible )
		{
			Cursor += (mouseDelta * sensitivity);
			Cursor = Cursor.Clamp( 0f, 1f );
		}

		ActiveChild?.BuildInput();

		CursorDirection = Screen.GetDirection( Screen.Size * Cursor );
		CameraPosition = CurrentView.Position;

		var plane = new Plane( Position, Vector3.Up );
		var trace = plane.Trace( new Ray( EyePosition, CursorDirection ), true );

		if ( trace.HasValue )
		{
			var direction = (trace.Value - Position).Normal;
			ViewAngles = direction.EulerAngles;
		}

		var startPosition = CameraPosition;
		var endPosition = CameraPosition + CursorDirection * 1000f;
		var cursor = Trace.Ray( startPosition, endPosition )
			.EntitiesOnly()
			.Run();

		var visible = Trace.Ray( EyePosition, cursor.EndPosition )
			.Ignore( this )
			.Ignore( ActiveChild )
			.Run();

		if ( visible.Fraction > 0.9f )
			HoveredEntity = cursor.Entity;
		else
			HoveredEntity = null;
	}

	public override void Respawn()
	{
		Controller = new MoveController
		{
			SprintSpeed = 200f,
			WalkSpeed = 100f
		};

		CameraMode = new TopDownCamera();
		Animator = new PlayerAnimator();

		EnableAllCollisions = true;
		EnableDrawing = true;
		LifeState = LifeState.Alive;
		Stamina = 100f;
		Health = 100f;
		Velocity = Vector3.Zero;
		WaterLevel = 0f;

		CreateHull();
		GiveInitialItems();
		InitializeHotbarWeapons();
		ResetCursor();

		base.Respawn();
	}

	public override void TakeDamage( DamageInfo info )
	{
		if ( info.Attacker is ForsakenPlayer )
		{
			if ( info.Hitbox.HasTag( "head" ) )
			{
				info.Damage *= 2f;
			}

			using ( Prediction.Off() )
			{
				var particles = Particles.Create( "particles/gameplay/player/taken_damage/taken_damage.vpcf", info.Position );
				particles.SetForward( 0, info.Force.Normal );
			}

			if ( info.Flags.HasFlag( DamageFlags.Blunt ) )
			{
				ApplyAbsoluteImpulse( info.Force );
			}
		}

		LastDamageTaken = info;
		base.TakeDamage( info );
	}

	public override void OnKilled()
	{
		BecomeRagdollOnClient( LastDamageTaken.Force, LastDamageTaken.BoneIndex );

		EnableAllCollisions = false;
		EnableDrawing = false;
		Controller = null;

		var itemsToDrop = FindItems<InventoryItem>().Where( i => i.DropOnDeath );

		foreach ( var item in itemsToDrop )
		{
			var entity = new ItemEntity();
			entity.Position = WorldSpaceBounds.Center + Vector3.Up * 64f;
			entity.SetItem( item );
			entity.ApplyLocalImpulse( Vector3.Random * 100f );
		}

		var weapons = Children.OfType<Weapon>().ToArray();

		foreach ( var weapon in weapons )
		{
			weapon.Delete();
		}

		base.OnKilled();
	}

	public override void ClientSpawn()
	{
		if ( IsLocalPawn )
		{
			Backpack.SetTransferHandler( GetBackpackTransferTarget );
			Equipment.SetTransferHandler( GetEquipmentTransferTarget );
			Hotbar.SetTransferHandler( GetHotbarTransferTarget );
		}

		base.ClientSpawn();
	}

	public override void FrameSimulate( Client cl )
	{
		SimulateConstruction();

		base.FrameSimulate( cl );
	}

	public override void Simulate( Client client )
	{
		base.Simulate( client );

		if ( Stamina <= 10f )
			IsOutOfBreath = true;
		else if ( IsOutOfBreath && Stamina >= 25f )
			IsOutOfBreath = false;

		Projectiles.Simulate();

		SimulateContextActions();
		SimulateHotbar();
		SimulateInventory();
		SimulateConstruction();
		SimulateDeployable();
		SimulateActiveChild( client, ActiveChild );
	}

	protected override void OnDestroy()
	{
		if ( IsServer )
		{
			InventorySystem.Remove( Hotbar, true );
			InventorySystem.Remove( Backpack, true );
			InventorySystem.Remove( Equipment, true );
		}

		base.OnDestroy();
	}

	private void SimulateContextActions()
	{
		if ( IsClient )
		{
			if ( HoveredEntity.IsValid() && HoveredEntity is IContextActionProvider actions )
			{
				var glow = HoveredEntity.Components.GetOrCreate<Glow>();
				glow.Enabled = true;
				glow.Color = actions.GlowColor;
				glow.Width = actions.GlowWidth;
			}

			if ( LastHoveredEntity.IsValid() && LastHoveredEntity != HoveredEntity )
			{
				var glow = LastHoveredEntity.Components.GetOrCreate<Glow>();
				glow.Enabled = false;
			}

			LastHoveredEntity = HoveredEntity;
		}
	}

	private void SimulateDeployable()
	{
		if ( GetActiveHotbarItem() is not DeployableItem deployable )
		{
			Deployable.ClearGhost();
			return;
		}

		var startPosition = CameraPosition;
		var endPosition = CameraPosition + CursorDirection * 1000f;
		var trace = Trace.Ray( startPosition, endPosition )
			.WithAnyTags( deployable.ValidTags )
			.Run();

		var model = Model.Load( deployable.Model );
		var collision = Trace.Box( model.PhysicsBounds, trace.EndPosition, trace.EndPosition ).Run();
		var isPositionValid = !collision.Hit && deployable.CanPlaceOn( trace.Entity );

		if ( IsClient )
		{
			var ghost = Deployable.GetOrCreateGhost( model );
			ghost.RenderColor = isPositionValid ? Color.Cyan.WithAlpha( 0.5f ) : Color.Red.WithAlpha( 0.5f );

			if ( !isPositionValid )
			{
				var cursor = Trace.Ray( startPosition, endPosition )
					.WorldOnly()
					.Run();

				ghost.Position = cursor.EndPosition;
			}
			else
			{
				ghost.Position = trace.EndPosition;
			}

			ghost.ResetInterpolation();
		}

		if ( Input.Released( InputButton.PrimaryAttack ) && isPositionValid )
		{
			if ( IsServer )
			{
				var entity = TypeLibrary.Create<Deployable>( deployable.Deployable );
				entity.Position = trace.EndPosition;
				entity.ResetInterpolation();
				deployable.Remove();
			}
		}
	}

	private void SimulateConstruction()
	{
		if ( GetActiveHotbarItem() is not ToolboxItem )
		{
			Structure.ClearGhost();
			return;
		}

		var structureType = TypeLibrary.GetDescriptionByIdent( StructureType );

		if ( structureType == null )
		{
			Structure.ClearGhost();
			return;
		}

		var trace = Trace.Ray( CameraPosition, CameraPosition + CursorDirection * 1000f )
			.WorldOnly()
			.Run();

		if ( !trace.Hit )
		{
			Structure.ClearGhost();
			return;
		}

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