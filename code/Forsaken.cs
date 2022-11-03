using Sandbox;
using System;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Forsaken : Game
{
	public override void ClientSpawn()
	{
		Local.Hud?.Delete( true );
		Local.Hud = new Hud();

		base.ClientSpawn();
	}

	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		var pawn = new Player( client );
		client.Pawn = pawn;
		pawn.Respawn();
	}

	public override void PostLevelLoaded()
	{
		Map.Entity.Tags.Add( "world" );

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
