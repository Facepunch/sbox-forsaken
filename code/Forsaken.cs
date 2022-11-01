using Sandbox;
using System;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Forsaken : Game
{
	public Forsaken()
	{
		if ( IsClient )
		{
			Local.Hud?.Delete( true );
			Local.Hud = new Hud();
		}
	}

	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		var pawn = new Player();
		client.Pawn = pawn;
		pawn.Respawn();
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
