using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public class PickupSpawner
{
	public static List<PickupSpawner> All { get; private set; } = new();

	private TimeUntil NextSpawnTime { get; set; }

	public int MinPickupsPerSpawn { get; set; } = 0;
	public int MaxPickupsPerSpawn { get; set; } = 100;
	public int MaxPickups { get; set; } = 100;
	public float Interval { get; set; } = 120f;
	public Vector3 Origin { get; set; }
	public float Range { get; set; } = 10000f;

	private Type Type { get; set; }

	public PickupSpawner()
	{
		Event.Register( this );
		NextSpawnTime = 0f;
		All.Add( this );
	}

	public void SetType<T>() where T : ResourcePickup
	{
		Type = typeof( T );
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		if ( Type is null || !NextSpawnTime ) return;

		var totalCount = Entity.All
			.OfType<ResourcePickup>()
			.Where( p => p.GetType() == Type )
			.Count();

		var availablePickupsToSpawn = MaxPickups - totalCount;

		if ( availablePickupsToSpawn > 0 )
		{
			var pickupsToSpawn = Game.Random.Int( Math.Min( MinPickupsPerSpawn, availablePickupsToSpawn ), Math.Min( MaxPickupsPerSpawn, availablePickupsToSpawn ) );
			var attemptsRemaining = 10000;

			while ( pickupsToSpawn > 0 && attemptsRemaining > 0 )
			{
				var position = Origin + new Vector3( Game.Random.Float( -1f, 1f ) * Range, Game.Random.Float( -1f, 1f ) * Range );
				var trace = Trace.Ray( position + Vector3.Up * 5000f, position + Vector3.Down * 5000f )
					.Run();

				if ( trace.Hit && trace.Entity.IsWorld )
				{
					var description = TypeLibrary.GetType( Type );
					var pickup = description.Create<ResourcePickup>();
					pickup.Position = trace.EndPosition;
					pickup.Rotation = Rotation.Identity.RotateAroundAxis( Vector3.Up, Game.Random.Float() * 360f );
					pickupsToSpawn--;
				}
				else
				{
					attemptsRemaining--;
				}
			}
		}

		NextSpawnTime = Interval;
	}
}
