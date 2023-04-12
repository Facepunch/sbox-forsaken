using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class ForsakenGame : GameManager
{
	public static ForsakenGame Entity => Current as ForsakenGame;
	public static string UniqueSaveId => Entity?.InternalSaveId ?? string.Empty;

	[ConVar.Replicated( "fsk.isometric" )]
	public static bool Isometric { get; set; } = true;

	[ConVar.Server( "fsk.autosave", Saved = true )]
	public static bool ShouldAutoSave { get; set; } = true;

	[ConVar.Server( "fsk.pvp" )]
	public static bool EnablePvP { get; set; } = true;

	[ConVar.Server( "fsk.pve" )]
	public static bool EnablePvE { get; set; } = true;

	private TimeUntil NextDespawnItems { get; set; }
	private TimeUntil NextAutoSave { get; set; }
	private IsometricCamera IsometricCamera { get; set; }
	private TopDownCamera TopDownCamera { get; set; }
	private bool HasLoadedWorld { get; set; }

	[Net] private string InternalSaveId { get; set; }

	public ForsakenGame() : base()
	{

	}

	public override void LoadSavedGame( SavedGame save )
	{
		Log.Info( "[Forsaken] Loading world..." );

		using var s = new MemoryStream( save.Data );
		using var r = new BinaryReader( s );

		PersistenceSystem.LoadAll( r );
	}

	public override void Spawn()
	{
		InventorySystem.Initialize();
		base.Spawn();
	}

	public override void ClientSpawn()
	{
		InventorySystem.Initialize();

		ItemTag.Register( "deployable", "Deployable", ItemColors.Deployable );
		ItemTag.Register( "consumable", "Consumable", ItemColors.Consumable );
		ItemTag.Register( "tool", "Tool", ItemColors.Tool );

		Game.RootPanel?.Delete( true );
		Game.RootPanel = new UI.Hud();

		IsometricCamera = new();
		TopDownCamera = new();

		base.ClientSpawn();
	}

	public override void ClientJoined( IClient client )
	{
		InventorySystem.ClientJoined( client );

		var pawn = All.OfType<ForsakenPlayer>()
			.Where( p => p.SteamId == client.SteamId )
			.FirstOrDefault();

		if ( !pawn.IsValid() )
		{
			pawn = new ForsakenPlayer();
			pawn.MakePawnOf( client );
			pawn.Respawn();
		}
		else
		{
			pawn.MakePawnOf( client );
		}

		PersistenceSystem.Load( pawn );

		base.ClientJoined( client );
	}

	public override void MoveToSpawnpoint( Entity pawn )
	{
		if ( pawn is ForsakenPlayer player && player.Bedroll.IsValid() )
		{
			player.Position = player.Bedroll.Position + Vector3.Up * 10f;
			return;
		}

		base.MoveToSpawnpoint( pawn );
	}

	public override void ClientDisconnect( IClient client, NetworkDisconnectionReason reason )
	{
		if ( client.Pawn is ForsakenPlayer player )
		{
			PersistenceSystem.Save( player );
		}

		InventorySystem.ClientDisconnected( client );

		base.ClientDisconnect( client, reason );
	}

	public override bool CanHearPlayerVoice( IClient source, IClient receiver )
	{
		if ( !source.IsValid() || !receiver.IsValid() ) return false;

		var a = source.Pawn as ForsakenPlayer;
		var b = source.Pawn as ForsakenPlayer;

		if ( !a.IsValid() || !b.IsValid() ) return false;

		return a.Position.Distance( b.Position ) <= 2000f;
	}

	public override void OnVoicePlayed( IClient cl )
	{
		cl.Voice.WantsStereo = false;
		base.OnVoicePlayed( cl );
	}

	public override void PostLevelLoaded()
	{
		Game.WorldEntity.Tags.Add( "world" );

		{
			var spawner = new LimitedSpawner();
			spawner.SetType<WoodPickup>();
			spawner.UseNavMesh = true;
			spawner.MaxTotal = 400;
			spawner.MinPerSpawn = 200;
			spawner.MaxPerSpawn = 300;
			spawner.Interval = 120f;
		}

		{
			var spawner = new LimitedSpawner();
			spawner.SetType<StonePickup>();
			spawner.UseNavMesh = true;
			spawner.MaxTotal = 300;
			spawner.MinPerSpawn = 100;
			spawner.MaxPerSpawn = 200;
			spawner.Interval = 180f;
		}

		{
			var spawner = new LimitedSpawner();
			spawner.SetType<MetalOrePickup>();
			spawner.UseNavMesh = true;
			spawner.MaxTotal = 200;
			spawner.MinPerSpawn = 100;
			spawner.MaxPerSpawn = 150;
			spawner.Interval = 300f;
		}

		{
			var spawner = new LimitedSpawner();
			spawner.SetType<PlantFiberPickup>();
			spawner.UseNavMesh = true;
			spawner.MaxTotal = 250;
			spawner.MinPerSpawn = 150;
			spawner.MaxPerSpawn = 200;
			spawner.Interval = 120f;
		}

		{
			var spawner = new LimitedSpawner();
			spawner.SetType<Deer>();
			spawner.UseNavMesh = true;
			spawner.MaxTotal = 20;
			spawner.MinPerSpawn = 1;
			spawner.MaxPerSpawn = 10;
			spawner.Interval = 120f;
		}

		{
			var spawner = new LimitedSpawner();
			spawner.SetType<Undead>();
			spawner.OnSpawned = ( e ) => (e as Undead)?.RiseFromGround();
			spawner.SpawnNearPlayers = true;
			spawner.TimeOfDayStart = 19.5f;
			spawner.TimeOfDayEnd = 7f;
			spawner.UseNavMesh = true;
			spawner.MaxTotal = 20;
			spawner.MinPerSpawn = 10;
			spawner.MaxPerSpawn = 20;
			spawner.Interval = 10f;
		}

		NextDespawnItems = 30f;
		HasLoadedWorld = true;
		NextAutoSave = 60f;

		base.PostLevelLoaded();
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		if ( HasLoadedWorld && NextAutoSave && ShouldAutoSave )
		{
			Log.Info( "[Forsaken] Saving world..." );
			PersistenceSystem.SaveAll();
			NextAutoSave = 60f;
		}

		if ( HasLoadedWorld && NextDespawnItems )
		{
			var items = All.OfType<ItemEntity>();

			foreach ( var item in items )
			{
				if ( item.TimeSinceSpawned >= 1800f )
				{
					item.Delete();
				}
			}

			NextDespawnItems = 30f;
		}

		InternalSaveId = PersistenceSystem.UniqueId;
	}

	[Event.Client.Frame]
	private void OnFrame()
	{
		if ( Isometric )
			IsometricCamera?.Update();
		else
			TopDownCamera?.Update();
	}
}
