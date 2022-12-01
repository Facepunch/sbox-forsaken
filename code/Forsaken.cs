using Sandbox;
using System;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Forsaken : Game
{
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

		Local.Hud?.Delete( true );
		Local.Hud = new UI.Hud();

		base.ClientSpawn();
	}

	public override void ClientJoined( Client client )
	{
		InventorySystem.ClientJoined( client );

		var pawn = new ForsakenPlayer( client );
		client.Pawn = pawn;
		pawn.Respawn();

		Log.Info( "Spawned Player" );

		PersistenceSystem.Load( pawn );

		base.ClientJoined( client );
	}

	public override void ClientDisconnect( Client client, NetworkDisconnectionReason reason )
	{
		if ( client.Pawn is ForsakenPlayer player )
		{
			PersistenceSystem.Save( player );
		}

		InventorySystem.ClientDisconnected( client );

		base.ClientDisconnect( client, reason );
	}

	public override void PostLevelLoaded()
	{
		Map.Entity.Tags.Add( "world" );

		{
			var spawner = new PickupSpawner();
			spawner.SetType<WoodPickup>();
			spawner.MaxPickups = 100;
			spawner.MaxPickupsPerSpawn = 20;
			spawner.MaxPickupsPerSpawn = 80;
			spawner.Interval = 60f;
		}

		{
			var spawner = new PickupSpawner();
			spawner.SetType<StonePickup>();
			spawner.MaxPickups = 70;
			spawner.MaxPickupsPerSpawn = 20;
			spawner.MaxPickupsPerSpawn = 60;
			spawner.Interval = 120f;
		}

		{
			var spawner = new PickupSpawner();
			spawner.SetType<MetalOrePickup>();
			spawner.MaxPickups = 50;
			spawner.MaxPickupsPerSpawn = 20;
			spawner.MaxPickupsPerSpawn = 60;
			spawner.Interval = 180f;
		}

		PersistenceSystem.LoadAll();

		base.PostLevelLoaded();
	}

	public override void MoveToSpawnpoint( Entity pawn )
	{
		var spawnpoints = All.OfType<SpawnPoint>();
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50f;
			pawn.Transform = tx;
		}
	}
}
