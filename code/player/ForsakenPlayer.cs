using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Forsaken.UI;
using Sandbox;
using Sandbox.Component;
using Sandbox.Diagnostics;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer : AnimatedEntity, IPersistence
{
	private class ActiveEffect
	{
		public ConsumableEffect Type { get; set; }
		public TimeUntil EndTime { get; set; }
		public float AmountGiven { get; set; }
	}

	public static ForsakenPlayer Me => Game.LocalPawn as ForsakenPlayer;

	[ConCmd.Server]
	public static void KillMe()
	{
		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer pl )
		{
			pl.TakeDamage( DamageInfo.FromBullet( pl.Position, Vector3.Zero, 1000f ) );
		}
	}

	[Net] public string DisplayName { get; private set; }
	[Net] public float Temperature { get; private set; }
	[Net] public float Calories { get; private set; }
	[Net] public float Hydration { get; private set; }
	[Net, Predicted] public float Stamina { get; private set; }
	[Net, Predicted] public bool IsOutOfBreath { get; private set; }
	[Net, Predicted] public int HotbarIndex { get; private set; }
	[Net] public TimedAction TimedAction { get; private set; }

	[Net] private NetInventoryContainer InternalBackpack { get; set; }
	public InventoryContainer Backpack => InternalBackpack.Value;

	[Net] private NetInventoryContainer InternalHotbar { get; set; }
	public InventoryContainer Hotbar => InternalHotbar.Value;

	[Net] private NetInventoryContainer InternalEquipment { get; set; }
	public InventoryContainer Equipment => InternalEquipment.Value;

	[ClientInput] public Vector3 CursorDirection { get; private set; }
	[ClientInput] public Vector3 CameraPosition { get; private set; }
	[ClientInput] public int ContextActionId { get; private set; }
	[ClientInput] public Entity HoveredEntity { get; private set; }
	[ClientInput] public string OpenContainerIds { get; private set; }
	[ClientInput] public string ChangeAmmoType { get; private set; }
	[ClientInput] public bool HasDialogOpen { get; private set; }

	[Net, Predicted] public Entity ActiveChild { get; set; }
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }
	[ClientInput] public int DeployableYaw { get; set; }
	public Angles OriginalViewAngles { get; private set; }

	public int MaxHealth => 100;
	public int MaxStamina => 100;
	public int MaxCalories => 300;
	public int MaxHydration => 200;

	public Dictionary<ArmorSlot, List<ArmorEntity>> Armor { get; private set; }
	public ProjectileSimulator Projectiles { get; private set; }
	public MoveController Controller { get; private set; }
	public Vector2 Cursor { get; set; }
	public DamageInfo LastDamageTaken { get; private set; }
	public bool HasTimedAction => TimedAction is not null;
	public bool IsSleeping => !Client.IsValid();

	[Net] private int StructureType { get; set; }
	[Net] public long SteamId { get; private set; }
	[Net] public Bedroll Bedroll { get; private set; }

	private TimeUntil NextCalculateTemperature { get; set; }
	private float CalculatedTemperature { get; set; }
	private List<IHeatEmitter> HeatEmitters { get; set; } = new();
	private TimeSince TimeSinceBackpackOpen { get; set; }
	private bool IsBackpackToggleMode { get; set; }
	private Entity LastHoveredEntity { get; set; }
	private List<ActiveEffect> ActiveEffects { get; set; } = new();
	private TimeSince TimeSinceLastKilled { get; set; }
	private Entity LastActiveChild { get; set; }

	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	[Net, Predicted]
	public Vector3 EyeLocalPosition { get; set; }

	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	[Net, Predicted]
	public Rotation EyeLocalRotation { get; set; }

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

	[ConCmd.Server( "fsk.player.structuretype" )]
	private static void SetStructureTypeCmd( int identity )
	{
		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer player )
		{
			player.StructureType = identity;
		}
	}

	[ConCmd.Server( "fsk.item.give" )]
	public static void GiveItemCmd( string itemId, int amount )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player )
			return;

		var definition = InventorySystem.GetDefinition( itemId );
		var totalToGive = amount;
		var stacksToGive = totalToGive / definition.MaxStackSize;
		var remainder = totalToGive % definition.MaxStackSize;

		for ( var i = 0; i < stacksToGive; i++ )
		{
			var item = InventorySystem.CreateItem( itemId );
			item.StackSize = item.MaxStackSize;
			player.TryGiveItem( item );
		}

		if ( remainder > 0 )
		{
			var item = InventorySystem.CreateItem( itemId );
			item.StackSize = (ushort)remainder;
			player.TryGiveItem( item );
		}
	}

	public ForsakenPlayer() : base()
	{
		Projectiles = new( this );

		if ( Game.IsServer )
		{
			CreateInventories();
			CraftingQueue = new List<CraftingQueueEntry>();
			HotbarIndex = 0;
			Armor = new();
		}

		Controller = new MoveController( this )
		{
			SprintSpeed = 200f,
			WalkSpeed = 100f
		};
	}

	public void MakePawnOf( long playerId )
	{
		SteamId = playerId;
	}

	public void SetBedroll( Bedroll bedroll )
	{
		Bedroll = bedroll;
	}

	public void MakePawnOf( IClient client )
	{
		Game.AssertServer();

		client.Pawn = this;

		Equipment.AddConnection( client );
		Backpack.AddConnection( client );
		Hotbar.AddConnection( client );

		DisplayName = client.Name;
		SteamId = client.SteamId;
	}

	public void SetAmmoType( string uniqueId )
	{
		Assert.NotNull( uniqueId );
		ChangeAmmoType = uniqueId;
	}

	public void SetStructureType( TypeDescription type )
	{
		Game.AssertClient();
		Assert.NotNull( type );
		SetStructureTypeCmd( type.Identity );
	}

	[ClientRpc]
	public void ResetCursor()
	{
		Cursor = new Vector2( 0.5f, 0.5f );
	}

	public IEnumerable<IClient> GetChatRecipients()
	{
		var clientsNearby = FindInSphere( Position, 4000f )
			.OfType<ForsakenPlayer>()
			.Select( p => p.Client );

		foreach ( var client in clientsNearby )
		{
			yield return client;
		}
	}

	public void ReduceStamina( float amount )
	{
		Stamina = Math.Max( Stamina - amount, 0f );
	}

	public void SetContextAction( ContextAction action )
	{
		ContextActionId = action.Hash;
	}

	public InventoryItem GetActiveHotbarItem()
	{
		if ( !IsHotbarSelected() ) return null;
		return Hotbar.GetFromSlot( (ushort)HotbarIndex );
	}

	public void GainStamina( float amount )
	{
		Stamina = Math.Min( Stamina + amount, 100f );
	}

	public void StartTimedAction( TimedActionInfo info )
	{
		TimedAction = new( info );
	}

	public void CancelTimedAction()
	{
		TimedAction = null;
	}

	public void AddEffect( ConsumableEffect effect )
	{
		if ( effect.Duration > 0f )
		{
			var instance = new ActiveEffect()
			{
				EndTime = effect.Duration,
				AmountGiven = 0f,
				Type = effect
			};

			ActiveEffects.Add( instance );
		}
		else
		{
			if ( effect.Target == ConsumableType.Calories )
				Calories = Math.Clamp( Calories + effect.Amount, 0f, MaxCalories );
			else if ( effect.Target == ConsumableType.Hydration )
				Hydration = Math.Clamp( Hydration + effect.Amount, 0f, MaxHydration );
			else if ( effect.Target == ConsumableType.Health )
				Health = Math.Clamp( Health + effect.Amount, 0f, MaxHealth );
			else if ( effect.Target == ConsumableType.Stamina )
				Stamina = Math.Clamp( Stamina + effect.Amount, 0f, MaxStamina );
		}
	}

	public virtual void Respawn()
	{
		EnableAllCollisions = true;
		EnableDrawing = true;
		LifeState = LifeState.Alive;
		Calories = 100f;
		Hydration = 30f;
		Stamina = 100f;
		Health = 100f;
		Velocity = Vector3.Zero;

		CreateHull();
		GiveInitialItems();
		InitializeWeapons();
		ResetCursor();

		ForsakenGame.Entity?.MoveToSpawnpoint( this );
		ResetInterpolation();
	}

	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		NextCalculateTemperature = 0f;
		EnableLagCompensation = true;

		Tags.Add( "player" );

		base.Spawn();
	}

	private TimeSince TimeSinceLastFootstep { get; set; }
	public override void OnAnimEventFootstep( Vector3 position, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( !Game.IsClient )
			return;

		if ( TimeSinceLastFootstep < 0.2f )
			return;

		volume *= GetFootstepVolume();

		TimeSinceLastFootstep = 0f;

		var tr = Trace.Ray( position, position + Vector3.Down * 20f )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	public override void BuildInput()
	{
		OriginalViewAngles = ViewAngles;
		InputDirection = Input.AnalogMove;

		var storage = UI.Storage.Current;
		var cooking = UI.Cooking.Current;

		if ( cooking.IsOpen && cooking.Cooker.IsValid() )
			OpenContainerIds = cooking.Cooker.Processor.GetContainerIdString();
		else if ( storage.IsOpen && storage.Container.IsValid() )
			OpenContainerIds = storage.Container.InventoryId.ToString();
		else
			OpenContainerIds = string.Empty;

		HasDialogOpen = UI.Dialog.IsActive();

		if ( Input.StopProcessing )
			return;

		var look = Input.AnalogLook;

		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		if ( Input.Released( InputButton.Reload ) )
		{
			DeployableYaw += 90;

			if ( DeployableYaw >= 360 )
				DeployableYaw = 0;
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;

		var mouseDelta = Input.MouseDelta / new Vector2( Screen.Width, Screen.Height );

		if ( !Mouse.Visible && !HasTimedAction )
		{
			Cursor += (mouseDelta * 20f * Time.Delta);
			Cursor = Cursor.Clamp( 0f, 1f );
		}

		ActiveChild?.BuildInput();

		CursorDirection = Screen.GetDirection( Screen.Size * Cursor );
		CameraPosition = Camera.Position;

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
			.WithTag( "hover" )
			.Ignore( this )
			.Size( 16f )
			.Run();

		if ( cursor.Entity.IsValid() )
		{
			var visible = Trace.Ray( EyePosition, cursor.Entity.WorldSpaceBounds.Center )
				.Ignore( this )
				.Ignore( ActiveChild )
				.Run();

			if ( !HasTimedAction && (visible.Entity == cursor.Entity || visible.Fraction > 0.9f) )
				HoveredEntity = cursor.Entity;
			else
				HoveredEntity = null;
		}
		else
		{
			HoveredEntity = null;
		}
	}

	public override void StartTouch( Entity other )
	{
		var emitter = other.FindParentOfType<IHeatEmitter>();

		if ( emitter.IsValid() && !HeatEmitters.Contains( emitter ) )
		{
			HeatEmitters.Add( emitter );
		}

		base.StartTouch( other );
	}

	public override void EndTouch( Entity other )
	{
		var emitter = other.FindParentOfType<IHeatEmitter>();

		if ( other is not null )
		{
			HeatEmitters.Remove( emitter );
		}

		base.EndTouch( other );
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

			if ( info.HasTag( "blunt" ) )
			{
				ApplyAbsoluteImpulse( info.Force );
			}
		}

		LastDamageTaken = info;

		if ( LifeState == LifeState.Dead )
			return;

		base.TakeDamage( info );

		this.ProceduralHitReaction( info );
	}

	public override void OnKilled()
	{
		BecomeRagdollOnServer( LastDamageTaken.Force, LastDamageTaken.BoneIndex );

		GameManager.Current?.OnKilled( this );

		EnableAllCollisions = false;
		EnableDrawing = false;
		LifeState = LifeState.Dead;

		ClearCraftingQueue();

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

	public override void FrameSimulate( IClient cl )
	{
		if ( LifeState == LifeState.Alive )
		{
			Controller?.FrameSimulate();
		}

		SimulateConstruction();
		SimulateDeployable();
	}

	public override void Simulate( IClient client )
	{
		if ( LifeState == LifeState.Dead )
		{
			if ( TimeSinceLastKilled > 3f && Game.IsServer )
			{
				Respawn();
			}
		}

		if ( LifeState == LifeState.Alive )
		{
			Controller?.Simulate();

			if ( Stamina <= 10f )
				IsOutOfBreath = true;
			else if ( IsOutOfBreath && Stamina >= 25f )
				IsOutOfBreath = false;

			Projectiles.Simulate();

			SimulateAnimation();

			if ( Game.IsServer )
			{
				SimulateNeeds();
				SimulateTimedAction();
			}

			SimulateCrafting();
			SimulateOpenContainers();

			if ( !HasDialogOpen )
			{
				if ( SimulateContextActions() )
					return;
			}

			SimulateAmmoType();
			SimulateHotbar();
			SimulateInventory();
			SimulateConstruction();
			SimulateDeployable();
			SimulateActiveChild( ActiveChild );
		}
	}

	protected virtual float GetFootstepVolume()
	{
		return Velocity.WithZ( 0 ).Length.LerpInverse( 0f, 200f ) * 0.5f;
	}

	protected virtual void CreateHull()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16f, -16f, 0f ), new Vector3( 16f, 16f, 72f ) );
		EnableHitboxes = true;
	}

	protected virtual void SimulateActiveChild( Entity child )
	{
		if ( Prediction.FirstTime )
		{
			if ( LastActiveChild != child )
			{
				OnActiveChildChanged( LastActiveChild, child );
				LastActiveChild = child;
			}
		}

		if ( !LastActiveChild.IsValid() )
			return;

		if ( LastActiveChild.IsAuthority )
		{
			LastActiveChild.Simulate( Client );
		}
	}

	protected virtual void OnActiveChildChanged( Entity previous, Entity next )
	{
		if ( previous is Weapon previousWeapon )
		{
			previousWeapon?.ActiveEnd( this, previousWeapon.Owner != this );
		}

		if ( next is Weapon nextWeapon )
		{
			nextWeapon?.ActiveStart( this );
		}
	}

	[Event.Tick.Server]
	protected virtual void ServerTick()
	{
		HeatEmitters.RemoveAll( e => !e.IsValid() );

		if ( NextCalculateTemperature )
		{
			CalculatedTemperature = TimeSystem.Temperature;
			CalculatedTemperature += Equipment.FindItems<ArmorItem>().Sum( i => i.TemperatureModifier );
			CalculatedTemperature += HeatEmitters.Sum( e =>
			{
				var distanceFraction = 1f - ((1f / e.EmissionRadius) * Position.Distance( e.Position ));
				return e.HeatToEmit * distanceFraction;
			} );

			NextCalculateTemperature = 1f;
		}

		Temperature = Temperature.LerpTo( CalculatedTemperature, Time.Delta * 2f );

		for ( var i = ActiveEffects.Count - 1; i >= 0; i-- )
		{
			var effect = ActiveEffects[i];
			var ticksPerSecond = (1f / Time.Delta) * effect.Type.Duration;
			var amountToGive = effect.Type.Amount / ticksPerSecond;

			if ( effect.Type.Target == ConsumableType.Calories )
				Calories = Math.Clamp( Calories + amountToGive, 0f, MaxCalories );
			else if ( effect.Type.Target == ConsumableType.Hydration )
				Hydration = Math.Clamp( Hydration + amountToGive, 0f, MaxHydration );
			else if ( effect.Type.Target == ConsumableType.Health )
				Health = Math.Clamp( Health + amountToGive, 0f, MaxHealth );
			else if ( effect.Type.Target == ConsumableType.Stamina )
				Stamina = Math.Clamp( Stamina + amountToGive, 0f, MaxStamina );

			if ( effect.EndTime )
			{
				ActiveEffects.RemoveAt( i );
			}
		}
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
		{
			InventorySystem.Remove( Hotbar, true );
			InventorySystem.Remove( Backpack, true );
			InventorySystem.Remove( Equipment, true );
		}

		base.OnDestroy();
	}

	private bool HasPrivilegeAt( Vector3 position )
	{
		var foundationsInRange = FindInSphere( position, Structure.PrivilegeRange ).OfType<Foundation>();

		foreach ( var foundation in foundationsInRange )
		{
			if ( foundation.Stockpile.IsValid() && !foundation.Stockpile.IsAuthorized( this ) )
			{
				return false;
			}
		}

		return true;
	}

	private void SimulateTimedAction()
	{
		if ( TimedAction is null ) return;

		if ( !InputDirection.IsNearZeroLength )
		{
			CancelTimedAction();
			return;
		}

		if ( TimedAction.EndTime )
		{
			TimedAction.OnFinished?.Invoke( this );
			TimedAction = null;
		}
	}

	private void SimulateNeeds()
	{
		var baseReduction = 0.02f;
		var calorieReduction = baseReduction;
		var hydrationReduction = baseReduction;

		if ( Velocity.Length > 0f )
		{
			var movementReduction = Velocity.Length.Remap( 0f, 300f, 0f, 0.075f );
			calorieReduction += movementReduction;
			hydrationReduction += movementReduction;
		}

		calorieReduction *= Temperature.Remap( -20, 0f, 4f, 2f );
		hydrationReduction *= Temperature.Remap( 0f, 40f, 0.5f, 4f );

		Calories = Math.Max( Calories - calorieReduction * Time.Delta, 0f );
		Hydration = Math.Max( Hydration - hydrationReduction * Time.Delta, 0f );
	}

	private void SimulateAmmoType()
	{
		if ( Game.IsServer )
		{
			if ( string.IsNullOrEmpty( ChangeAmmoType ) )
				return;

			var weapon = ActiveChild as Weapon;

			if ( weapon.IsValid() )
			{
				var definition = InventorySystem.GetDefinition( ChangeAmmoType ) as AmmoItem;

				if ( definition.IsValid() )
				{
					weapon.SetAmmoDefinition( definition );
					weapon.Reload();
				}
			}
		}

		ChangeAmmoType = string.Empty;
	}

	private void SimulateOpenContainers()
	{
		if ( Game.IsClient ) return;

		var viewer = Client.Components.Get<InventoryViewer>();
		viewer.ClearContainers();

		if ( string.IsNullOrEmpty( OpenContainerIds ) ) return;

		var split = OpenContainerIds.Split( ',' );

		foreach ( var id in split )
		{
			if ( ulong.TryParse( id, out var value ) )
			{
				var container = InventorySystem.Find( value );

				if ( container.IsValid() )
					viewer.AddContainer( container );
			}
		}
	}

	private bool SimulateContextActions()
	{
		var actions = HoveredEntity as IContextActionProvider;
		var actionId = ContextActionId;

		if ( Game.IsClient )
		{
			if ( actions.IsValid() )
			{
				var glow = HoveredEntity.Components.GetOrCreate<Glow>();
				glow.Enabled = true;
				glow.Width = actions.GlowWidth;

				if ( Position.Distance( actions.Position ) <= actions.InteractionRange )
					glow.Color = actions.GlowColor;
				else
					glow.Color = Color.Gray;
			}

			if ( LastHoveredEntity.IsValid() && LastHoveredEntity != HoveredEntity )
			{
				var glow = LastHoveredEntity.Components.GetOrCreate<Glow>();
				glow.Enabled = false;
			}

			LastHoveredEntity = HoveredEntity;

			ContextActionId = 0;
		}

		if ( actions.IsValid() && Position.Distance( actions.Position ) <= actions.InteractionRange )
		{
			if ( actionId != 0 )
			{
				var allActions = IContextActionProvider.GetAllActions( this, actions );
				var action = allActions.Where( a => a.IsAvailable( this ) && a.Hash == actionId ).FirstOrDefault();

				if ( action.IsValid() )
				{
					actions.OnContextAction( this, action );
				}
			}

			return true;
		}

		return false;
	}

	private void SimulateDeployable()
	{
		var deployable = GetActiveHotbarItem() as DeployableItem;

		if ( !deployable.IsValid() || deployable.IsStructure )
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
		var hitPosition = trace.EndPosition + Vector3.Up * 4f;
		var isWithinSight = CanSeePosition( hitPosition );
		var isWithinRange = IsPlacementRange( hitPosition );
		var isAuthorized = HasPrivilegeAt( hitPosition );

		if ( Game.IsClient )
		{
			var ghost = Deployable.GetOrCreateGhost( model );
			ghost.Rotation = Rotation.FromYaw( DeployableYaw );
			ghost.Position = hitPosition;

			var collision = Trace.Body( ghost.PhysicsBody, ghost.Position ).Run();
			var isPositionValid = !collision.Hit && deployable.CanPlaceOn( trace.Entity );

			if ( !isAuthorized || !isPositionValid || !isWithinSight || !isWithinRange )
			{
				var cursor = Trace.Ray( startPosition, endPosition )
					.WorldOnly()
					.Run();

				ghost.RenderColor = Color.Red.WithAlpha( 0.5f );
				ghost.Position = cursor.EndPosition + Vector3.Up * 4f;
			}
			else
			{
				ghost.RenderColor = Color.Cyan.WithAlpha( 0.5f );
			}

			ghost.ResetInterpolation();
		}

		if ( Input.Released( InputButton.PrimaryAttack ) )
		{
			if ( Game.IsServer )
			{
				if ( isAuthorized && isWithinRange && isWithinSight )
				{
					var ghost = Deployable.GetOrCreateGhost( model );
					ghost.Position = hitPosition;
					ghost.Rotation = Rotation.FromYaw( DeployableYaw );
					ghost.PhysicsBody.Position = ghost.Position;
					ghost.PhysicsBody.Rotation = ghost.Rotation;
					ghost.ResetInterpolation();

					var collision = Trace.Body( ghost.PhysicsBody, ghost.Position ).Run();
					var isPositionValid = !collision.Hit && deployable.CanPlaceOn( trace.Entity );

					if ( isPositionValid )
					{
						var entity = TypeLibrary.Create<Deployable>( deployable.Deployable );
						entity.Transform = ghost.Transform;

						entity.ResetInterpolation();
						entity.OnPlacedByPlayer( this, trace );
						deployable.StackSize--;

						if ( !string.IsNullOrEmpty( deployable.PlaceSoundName ) )
						{
							Sound.FromWorld( To.Everyone, deployable.PlaceSoundName, trace.EndPosition );
						}
					}
					else
					{
						Thoughts.Show( To.Single( this ), "invalid_placement", Game.Random.FromArray( InvalidPlacementThoughts ) );
					}

					Deployable.ClearGhost();
				}
				else if ( !isWithinRange)
				{
					Thoughts.Show( To.Single( this ), "out_of_range", Game.Random.FromArray( OutOfRangeThoughts ) );
				}
				else if ( !isWithinSight )
				{
					Thoughts.Show( To.Single( this ), "out_of_sight", Game.Random.FromArray( OutOfSightThoughts ) );
				}
				else
				{
					Thoughts.Show( To.Single( this ), "unauthorized", Game.Random.FromArray( UnauthorizedThoughts ) );
				}
			}
		}
	}

	private bool CanSeePosition( Vector3 position )
	{
		var trace = Trace.Ray( EyePosition, position )
			.WithAnyTags( "solid" )
			.Run();

		return trace.Fraction >= 0.9f;
	}

	private bool IsPlacementRange( Vector3 position )
	{
		return position.Distance( Position ) < 150f;
	}

	private void SimulateConstruction()
	{
		var item = GetActiveHotbarItem();
		var deployable = item as DeployableItem;

		if ( item is not ToolboxItem && ( !deployable.IsValid() || !deployable.IsStructure ) )
		{
			Structure.ClearGhost();
			return;
		}

		var structureType = TypeLibrary.GetTypeByIdent( StructureType );

		if ( deployable.IsValid() )
		{
			structureType = TypeLibrary.GetType( deployable.Deployable );
		}

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

		var isAuthorized = HasPrivilegeAt( trace.EndPosition );

		if ( Game.IsClient )
		{
			var ghost = Structure.GetOrCreateGhost( structureType );
			var match = ghost.LocateSocket( trace.EndPosition );
			var isValid = isAuthorized && Structure.CanAfford( this, structureType ) && IsPlacementRange( trace.EndPosition ) && CanSeePosition( trace.EndPosition );

			if ( match.IsValid )
			{
				ghost.SnapToSocket( match );
			}
			else
			{
				ghost.Position = trace.EndPosition;
				ghost.ResetInterpolation();

				if ( ghost.RequiresSocket || !ghost.IsValidPlacement( ghost.Position, trace.Normal ) )
					isValid = false;
			}

			if ( isValid )
				ghost.RenderColor = Color.Cyan.WithAlpha( 0.5f );
			else
				ghost.RenderColor = Color.Red.WithAlpha( 0.5f );
		}

		if ( Prediction.FirstTime && Input.Released( InputButton.PrimaryAttack ) )
		{
			Structure.ClearGhost();

			if ( !Game.IsServer ) return;

			if ( !isAuthorized )
			{
				Thoughts.Show( To.Single( this ), "unauthorized", Game.Random.FromArray( UnauthorizedThoughts ) );
				return;
			}

			if ( !Structure.CanAfford( this, structureType ) )
			{
				Thoughts.Show( To.Single( this ), "missing_items", Game.Random.FromArray( MissingItemsThoughts ) );
				return;
			}

			if ( !IsPlacementRange( trace.EndPosition ) )
			{
				Thoughts.Show( To.Single( this ), "out_of_range", Game.Random.FromArray( OutOfRangeThoughts ) );
				return;
			}

			if ( !CanSeePosition( trace.EndPosition ) )
			{
				Thoughts.Show( To.Single( this ), "out_of_sight", Game.Random.FromArray( OutOfSightThoughts ) );
				return;
			}

			var structure = structureType.Create<Structure>();

			if ( structure.IsValid() )
			{
				var isValid = false;
				var match = structure.LocateSocket( trace.EndPosition );

				if ( match.IsValid )
				{
					match.Ours.Connect( match.Theirs );
					structure.SnapToSocket( match );
					structure.OnConnected( match.Ours, match.Theirs );
					structure.OnPlacedByPlayer( this );
					isValid = true;

					var soundName = structure.PlaceSoundName;

					if ( deployable.IsValid() && !string.IsNullOrEmpty( deployable.PlaceSoundName ) )
						soundName = deployable.PlaceSoundName;

					if ( !string.IsNullOrEmpty( soundName ) )
					{
						Sound.FromWorld( To.Everyone, soundName, trace.EndPosition );
					}
				}
				else if ( !structure.RequiresSocket )
				{
					structure.Position = trace.EndPosition;
					isValid = structure.IsValidPlacement( structure.Position, trace.Normal );
				}

				if ( !isValid )
				{
					Thoughts.Show( To.Single( this ), "invalid_placement", Game.Random.FromArray( InvalidPlacementThoughts ) );
					structure.Delete();
				}
				else
				{
					var costs = Structure.GetCostsFor( structureType );

					foreach ( var kv in costs )
					{
						TakeItems( kv.Key, kv.Value );
					}

					if ( deployable.IsValid() )
					{
						deployable.StackSize--;
					}
				}
			}
		}
	}
}
