using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Forsaken.UI;
using Sandbox;
using Sandbox.Component;
using Sandbox.Diagnostics;

namespace Facepunch.Forsaken;

public partial class ForsakenPlayer : AnimatedEntity, IPersistence, INametagProvider, IDamageable
{
	private class ActiveEffect
	{
		public ConsumableEffect Type { get; set; }
		public TimeUntil EndTime { get; set; }
		public float AmountGiven { get; set; }
	}

	public static ForsakenPlayer Me => Game.LocalPawn as ForsakenPlayer;

	public static Glow AddObscuredGlow( ModelEntity entity )
	{
		var glow = entity.Components.Create<Glow>();
		glow.Width = 0.25f;
		glow.Color = Color.Transparent;
		glow.InsideObscuredColor = Color.White.WithAlpha( 0.8f );
		glow.ObscuredColor = Color.Black.WithAlpha( 0.5f );
		return glow;
	}

	[ConCmd.Server]
	public static void StarveMe()
	{
		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer pl )
		{
			pl.TakeDamage( DamageInfo.FromBullet( pl.Position, Vector3.Random * 80f, 1000f ).WithTag( "hunger" ) );
		}
	}

	[ConCmd.Server]
	public static void KillMe()
	{
		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer pl )
		{
			pl.TakeDamage( DamageInfo.FromBullet( pl.Position, Vector3.Random * 80f, 1000f ) );
		}
	}

	[ConCmd.Server]
	public static void RemoveMapMarker( string csv )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player ) return;

		var position = csv.ToVector3();

		foreach ( var marker in player.Markers )
		{
			if ( marker.Position.Distance( position ) <= 100f )
			{
				player.Markers.Remove( marker );
				break;
			}
		}

		player.RemoveMapMarker( To.Single( player ), position );
	}

	[ConCmd.Server]
	public static void AddMapMarker( string csv, string hex )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player ) return;

		var position = csv.ToVector3();
		var color = Color.Parse( hex ).Value.WithAlpha( 1f );

		var marker = new MapMarker();
		marker.Position = position;
		marker.Color = color;

		player.Markers.Add( marker );

		player.AddMapMarker( To.Single( player ), position, color );
	}

	[Net] public string DisplayName { get; private set; }
	[Net] public float Temperature { get; private set; }
	[Net] public float Calories { get; private set; }
	[Net] public float Hydration { get; private set; }
	[Net, Predicted] public float Stamina { get; private set; }
	[Net, Predicted] public bool IsOutOfBreath { get; private set; }
	[Net, Predicted] public int HotbarIndex { get; private set; }
	[Net] public TimedAction TimedAction { get; private set; }

	public List<MapMarker> Markers { get; private set; } = new List<MapMarker>();

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

	public float MaxHealth => 100;
	public float MaxStamina => 100;
	public float MaxCalories => 300;
	public float MaxHydration => 200;

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

	public HashSet<BaseTrigger> InsideZones { get; private set; } = new();
	public Color? NametagColor => null;
	public bool ShowNametag => LifeState == LifeState.Alive && ( Nametags.ShowOwnNametag || !IsLocalPawn );
	public bool IsInactive => IsSleeping;

	private TimeUntil NextCalculateTemperature { get; set; }
	private float CalculatedTemperature { get; set; }
	private List<IHeatEmitter> HeatEmitters { get; set; } = new();
	private TimeSince TimeSinceBackpackOpen { get; set; }
	private bool IsBackpackToggleMode { get; set; }
	private List<ActiveEffect> ActiveEffects { get; set; } = new();
	private TimeSince TimeSinceLastKilled { get; set; }
	private TimeUntil NextNeedsDamage { get; set; }
	private TimeUntil NextNeedsWarning { get; set; }
	private TimeUntil NextNeedsAlert { get; set; }
	private Glow GlowComponent { get; set; }
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

	public bool HasPrivilegeAt( Vector3 position )
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

	public void MakePawnOf( IClient client )
	{
		Game.AssertServer();

		client.Pawn = this;

		Equipment.AddConnection( client );
		Backpack.AddConnection( client );
		Hotbar.AddConnection( client );

		DisplayName = client.Name;
		SteamId = client.SteamId;

		foreach ( var marker in Markers )
		{
			AddMapMarker( To.Single( client ), marker.Position, marker.Color );
		}
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

	public bool IsHeadshotTarget( ForsakenPlayer other, DamageInfo info )
	{
		if ( !ForsakenGame.Isometric )
		{
			return info.Hitbox.HasTag( "head" );
		}

		var startPosition = CameraPosition;
		var endPosition = CameraPosition + CursorDirection * 1000f;
		var cursor = Trace.Ray( startPosition, endPosition )
			.EntitiesOnly()
			.UseHitboxes()
			.WithoutTags( "trigger" )
			.WithTag( "player" )
			.Size( 4f )
			.Run();

		if ( cursor.Entity == other && cursor.Hitbox.HasTag( "head" ) )
		{
			return true;
		}

		return false;
	}

	[ClientRpc]
	public void AddMapMarker( Vector3 position, Color color )
	{
		Markers.Add( new MapMarker { Position = position, Color = color } );
	}

	[ClientRpc]
	public void RemoveMapMarker( Vector3 position )
	{
		foreach ( var marker in Markers )
		{
			if ( marker.Position.Distance( position ) <= 100f )
			{
				Markers.Remove( marker );
				break;
			}
		}
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

	public bool IsActiveHotbarItem( InventoryItem item )
	{
		return GetActiveHotbarItem() == item;
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
		Game.AssertServer();

		CancelTimedAction();

		TimedAction = new( info );
		TimedAction.StartSound();
	}

	public void CancelTimedAction()
	{
		Game.AssertServer();

		TimedAction?.StopSound();
		TimedAction = null;
	}

	public void ClearEffects()
	{
		ActiveEffects.Clear();
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

	public virtual bool CanStaminaRegenerate()
	{
		return Hydration >= 0f;
	}

	public virtual bool IsAiming()
	{
		return ForsakenGame.Isometric && Input.Down( InputButton.SecondaryAttack );
	}

	public virtual void Respawn()
	{
		TimeSinceLastKilled = 0f;
		EnableAllCollisions = true;
		EnableDrawing = true;
		LifeState = LifeState.Alive;
		Calories = 90f;
		Hydration = 60f;
		Stamina = MaxStamina;
		Health = MaxHealth;
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

	public override void ClientSpawn()
	{
		if ( IsLocalPawn )
		{
			GlowComponent = AddObscuredGlow( this );
		}

		base.ClientSpawn();
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
			.WithoutTags( "trigger" )
			.Radius( 1f )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	public override void BuildInput()
	{
		OriginalViewAngles = ViewAngles;
		InputDirection = Input.AnalogMove;

		var trader = UI.Trading.Current;
		var storage = UI.Storage.Current;
		var cooking = UI.Cooking.Current;
		var recycling = UI.Recycling.Current;

		if ( trader.IsOpen && trader.Trader.IsValid() )
			OpenContainerIds = trader.Inventory.ContainerId.ToString();
		else if ( recycling.IsOpen && recycling.Recycler.IsValid() )
			OpenContainerIds = recycling.Recycler.Processor.GetContainerIdString();
		else if ( cooking.IsOpen && cooking.Cooker.IsValid() )
			OpenContainerIds = cooking.Cooker.Processor.GetContainerIdString();
		else if ( storage.IsOpen && storage.Container.IsValid() )
			OpenContainerIds = storage.Container.ContainerId.ToString();
		else
			OpenContainerIds = string.Empty;

		HasDialogOpen = UI.Dialog.IsActive();

		if ( Input.StopProcessing )
			return;

		if ( Input.Released( InputButton.Reload ) )
		{
			DeployableYaw += 90;

			if ( DeployableYaw >= 360 )
				DeployableYaw = 0;
		}

		var mouseDelta = Input.MouseDelta / new Vector2( Screen.Width, Screen.Height );

		if ( !Mouse.Visible && !HasTimedAction )
		{
			Cursor += (mouseDelta * 20f * Time.Delta);
			Cursor = Cursor.Clamp( 0f, 1f );
		}

		ActiveChild?.BuildInput();

		CursorDirection = Screen.GetDirection( Screen.Size * Cursor );
		CameraPosition = Camera.Position;

		if ( IsAiming() )
		{
			var trace = Trace.Ray( CameraPosition, CameraPosition + CursorDirection * 3000f )
				.Ignore( this )
				.Run();

			var direction = (trace.EndPosition - Position).Normal;
			ViewAngles = direction.EulerAngles;
		}
		else
		{
			var plane = new Plane( Position, Vector3.Up );
			var trace = plane.Trace( new Ray( CameraPosition, CursorDirection ), true );

			if ( trace.HasValue )
			{
				var direction = (trace.Value - Position).Normal;
				ViewAngles = direction.EulerAngles;
			}
		}

		ViewAngles = ViewAngles.WithPitch( 0f );

		var startPosition = CameraPosition;
		var endPosition = CameraPosition + CursorDirection * 3000f;
		var query = Trace.Ray( startPosition, endPosition )
			.EntitiesOnly()
			.WithoutTags( "trigger" )
			.WithTag( "hover" )
			.Ignore( this )
			.Size( 16f );

		var hotbarItem = GetActiveHotbarItem();

		if ( hotbarItem is not HammerItem )
		{
			query = query.WithoutTags( "hammer" );
		}

		var cursor = query.Run();

		if ( !IsAiming() && cursor.Entity.IsValid() )
		{
			var visible = Trace.Ray( EyePosition, cursor.Entity.WorldSpaceBounds.Center )
				.WithoutTags( "trigger" )
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
		if ( info.Attacker is ForsakenPlayer attacker )
		{
			// Early out if PvP isn't enabled for this session.
			if ( !ForsakenGame.EnablePvP ) return;

			if ( info.HasTag( "bullet" ) )
			{
				if ( attacker.IsHeadshotTarget( this, info ) )
				{
					Sound.FromScreen( To.Single( attacker ), "hitmarker.headshot" );
					info.Damage *= 2f;
				}
				else
				{
					Sound.FromScreen( To.Single( attacker ), "hitmarker.hit" );
				}

				using ( Prediction.Off() )
				{
					PlaySound( "melee.hitflesh" );
				}
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

		var protection = Equipment.FindItems<ArmorItem>()
			.Where( i => info.HasAnyTag( i.DamageTags.ToArray() ) )
			.Where( i => info.Hitbox.HasTag( i.DamageHitbox ) )
			.Sum( i => i.DamageProtection );

		var multiplier = 1f - ((1f / 100f) * protection);

		info.Damage *= multiplier;

		LastDamageTaken = info;

		if ( LifeState == LifeState.Dead )
			return;

		base.TakeDamage( info );

		this.ProceduralHitReaction( info );
	}

	public override void OnKilled()
	{
		BecomeRagdollOnServer( LastDamageTaken.Force, LastDamageTaken.BoneIndex );

		Death.Show( this, LastDamageTaken, TimeSinceLastKilled.Relative.CeilToInt() );

		GameManager.Current?.OnKilled( this );

		TimeSinceLastKilled = 0f;
		EnableAllCollisions = false;
		EnableDrawing = false;
		LifeState = LifeState.Dead;

		ClearCraftingQueue();
		ClearEffects();

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
			if ( TimeSinceLastKilled > 5f && Game.IsServer )
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

			SimulateConsumable();
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

		if ( LifeState == LifeState.Dead )
		{
			return;
		}

		if ( NextNeedsWarning )
		{
			if ( Calories > 0f && Calories < 20f && Game.Random.Float() > 0.5f )
				Thoughts.Show( To.Single( this ), "needs_warning", Game.Random.FromArray( HungryThoughts ) );

			if ( Hydration > 0f && Hydration < 20f && Game.Random.Float() > 0.5f )
				Thoughts.Show( To.Single( this ), "needs_warning", Game.Random.FromArray( ThirstyThoughts ) );

			NextNeedsWarning = 60f;
		}

		if ( NextNeedsDamage && ( Calories <= 0f || Hydration <= 0f ) )
		{
			if ( Calories <= 0f )
			{
				var damage = new DamageInfo()
					.WithTag( "hunger" )
					.WithPosition( Position );

				damage.Damage = 1f;
				TakeDamage( damage );

				if ( NextNeedsAlert && Game.Random.Float() > 0.5f )
				{
					Thoughts.Show( To.Single( this ), "needs", Game.Random.FromArray( StarvingThoughts ) );
					NextNeedsAlert = 30f;
				}
			}

			if ( Hydration <= 0f )
			{
				var damage = new DamageInfo()
					.WithTag( "thirst" )
					.WithPosition( Position );

				damage.Damage = 1f;
				TakeDamage( damage );

				if ( NextNeedsAlert && Game.Random.Float() > 0.5f )
				{
					Thoughts.Show( To.Single( this ), "needs", Game.Random.FromArray( DehydrationThoughts ) );
					NextNeedsAlert = 30f;
				}
			}

			NextNeedsDamage = 4f;
		}

		if ( NextCalculateTemperature )
		{
			CalculatedTemperature = TimeSystem.Temperature;

			CalculatedTemperature += InsideZones.OfType<TemperatureZone>().Sum( e =>
			{
				return e.Temperature;
			} );
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

		if ( IsSleeping )
		{
			SimulateSleeping();
		}
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
		{
			InventorySystem.Remove( Hotbar );
			InventorySystem.Remove( Backpack );
			InventorySystem.Remove( Equipment );
		}

		base.OnDestroy();
	}

	private void SimulateSleeping()
	{
		var trace = Trace.Ray( Position + Vector3.Up * 8f, Position + Vector3.Down * 100f )
			.WithoutTags( "trigger" )
			.Ignore( this )
			.Run();

		EyeLocalPosition = Vector3.Up * 72f;
		GroundEntity = trace.Entity;

		SimulateAnimation();
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
			TimedAction.StopSound();
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

	private List<IContextActionProvider> LastEntitiesInRange { get; set; } = new();

	private bool SimulateContextActions()
	{
		var actions = HoveredEntity as IContextActionProvider;
		var actionId = ContextActionId;

		if ( Game.IsClient )
		{
			var entities = FindInSphere( Position, 500f ).OfType<IContextActionProvider>();

			foreach ( var entity in LastEntitiesInRange )
			{
				var glow = entity.Components.GetOrCreate<Glow>();
				glow.Enabled = false;
			}

			LastEntitiesInRange.Clear();

			foreach ( var entity in entities )
			{
				if ( Position.Distance( entity.Position ) > entity.InteractionRange * 3f )
					continue;

				if ( !entity.AlwaysGlow )
					continue;

				var glow = entity.Components.GetOrCreate<Glow>();
				glow.InsideObscuredColor = entity.GlowColor.WithAlpha( 0.05f );
				glow.Color = entity.GlowColor.WithAlpha( 0.1f );
				glow.Width = 0.15f;
				glow.Enabled = true;

				LastEntitiesInRange.Add( entity );
			}

			if ( actions.IsValid() && Position.Distance( actions.Position ) <= actions.InteractionRange )
			{
				var glow = HoveredEntity.Components.GetOrCreate<Glow>();
				glow.InsideObscuredColor = actions.GlowColor.WithAlpha( 0.8f );
				glow.Color = actions.GlowColor;
				glow.Width = 0.2f;
				glow.Enabled = true;

				LastEntitiesInRange.Add( actions );
			}

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
					return true;
				}
			}
		}

		return false;
	}

	private void SimulateConsumable()
	{
		var consumable = GetActiveHotbarItem() as ConsumableItem;

		if ( consumable.IsValid() && Input.Released( InputButton.PrimaryAttack ) )
		{
			consumable.Consume( this );
		}
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
		var endPosition = CameraPosition + CursorDirection * 3000f;
		var trace = Trace.Ray( startPosition, endPosition )
			.WithoutTags( "trigger" )
			.WithAnyTags( deployable.ValidTags )
			.Run();

		if ( !trace.Hit )
		{
			Deployable.ClearGhost();
			return;
		}

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

			var isPositionValid = !Deployable.IsCollidingWithWorld( ghost ) && deployable.CanPlaceOn( trace.Entity );

			if ( !isAuthorized || !isPositionValid || !isWithinSight || !isWithinRange )
			{
				var cursor = Trace.Ray( startPosition, endPosition )
					.WithoutTags( "trigger" )
					.WorldOnly()
					.Run();

				if ( isAuthorized && isPositionValid )
					ghost.RenderColor = Color.Orange.WithAlpha( 0.5f );
				else
					ghost.RenderColor = Color.Red.WithAlpha( 0.5f );

				ghost.Position = cursor.EndPosition + Vector3.Up * 4f;
			}
			else
			{
				ghost.RenderColor = Color.Cyan.WithAlpha( 0.5f );
			}

			var glow = ghost.Components.GetOrCreate<Glow>();
			glow.InsideObscuredColor = ghost.RenderColor;

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

					var isPositionValid = !Deployable.IsCollidingWithWorld( ghost ) && deployable.CanPlaceOn( trace.Entity );

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
			.WithoutTags( "trigger" )
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

		var trace = Trace.Ray( CameraPosition, CameraPosition + CursorDirection * 3000f )
			.WithoutTags( "trigger" )
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
			var isCollisionError = false;
			var isWithinRange = IsPlacementRange( trace.EndPosition );
			var isWithinSight = CanSeePosition( trace.EndPosition );
			var isValid = isAuthorized && Structure.CanAfford( this, structureType ) && isWithinRange && isWithinSight;

			if ( match.IsValid )
			{
				ghost.SnapToSocket( match );
			}
			else
			{
				ghost.Position = trace.EndPosition;
				ghost.ResetInterpolation();

				if ( ghost.RequiresSocket || !ghost.IsValidPlacement( ghost.Position, trace.Normal ) )
				{
					isCollisionError = true;
					isValid = false;
				}
			}

			if ( ghost.IsCollidingWithWorld() )
			{
				isCollisionError = true;
				isValid = false;
			}

			if ( isValid )
				ghost.RenderColor = Color.Cyan.WithAlpha( 0.5f );
			else if ( isCollisionError )
				ghost.RenderColor = Color.Red.WithAlpha( 0.5f );
			else
				ghost.RenderColor = Color.Orange.WithAlpha( 0.5f );

			var glow = ghost.Components.GetOrCreate<Glow>();
			glow.InsideObscuredColor = ghost.RenderColor;
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
					structure.SnapToSocket( match );
					isValid = true;
				}
				else if ( !structure.RequiresSocket )
				{
					structure.Position = trace.EndPosition;
					structure.PhysicsBody.Transform = structure.Transform;
					structure.ResetInterpolation();
					isValid = structure.IsValidPlacement( structure.Position, trace.Normal );
				}

				if ( structure.IsCollidingWithWorld() )
				{
					isValid = false;
				}

				if ( !isValid )
				{
					Thoughts.Show( To.Single( this ), "invalid_placement", Game.Random.FromArray( InvalidPlacementThoughts ) );
					structure.Delete();
				}
				else
				{
					if ( match.IsValid )
					{
						match.Ours.Connect( match.Theirs );
						structure.OnConnected( match.Ours, match.Theirs );
					}

					structure.OnPlacedByPlayer( this );

					var soundName = structure.PlaceSoundName;

					if ( deployable.IsValid() && !string.IsNullOrEmpty( deployable.PlaceSoundName ) )
						soundName = deployable.PlaceSoundName;

					if ( !string.IsNullOrEmpty( soundName ) )
					{
						Sound.FromWorld( To.Everyone, soundName, trace.EndPosition );
					}

					var costs = Structure.GetCostsFor( structureType );

					foreach ( var kv in costs )
					{
						TakeItems( kv.Key, kv.Value );
					}

					if ( deployable.IsValid() )
					{
						deployable.StackSize--;
					}

					Navigation.Update( structure.Position, 256f );
				}
			}
		}
	}
}
