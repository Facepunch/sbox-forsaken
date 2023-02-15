using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public class LimitedSpawner
{
	public static List<LimitedSpawner> All { get; private set; } = new();

	private TimeUntil NextSpawnTime { get; set; }

	public int MinPerSpawn { get; set; } = 0;
	public int MaxPerSpawn { get; set; } = 100;
	public int MaxTotal { get; set; } = 100;
	public float Interval { get; set; } = 120f;
	public Vector3 Origin { get; set; }
	public float Range { get; set; } = 10000f;

	private Type Type { get; set; }

	public LimitedSpawner()
	{
		Event.Register( this );
		NextSpawnTime = 0f;
		All.Add( this );
	}

	public void SetType<T>() where T : ILimitedSpawner
	{
		Type = typeof( T );
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		if ( Type is null || !NextSpawnTime ) return;

		var totalCount = Entity.All
			.OfType<ILimitedSpawner>()
			.Where( p => p.GetType() == Type )
			.Count();

		var availableToSpawn = MaxTotal - totalCount;

		if ( availableToSpawn > 0 )
		{
			var amountToSpawn = Game.Random.Int( Math.Min( MinPerSpawn, availableToSpawn ), Math.Min( MaxPerSpawn, availableToSpawn ) );
			var attemptsRemaining = 10000;

			while ( amountToSpawn > 0 && attemptsRemaining > 0 )
			{
				var position = Origin + new Vector3( Game.Random.Float( -1f, 1f ) * Range, Game.Random.Float( -1f, 1f ) * Range );
				var trace = Trace.Ray( position + Vector3.Up * 5000f, position + Vector3.Down * 5000f )
					.WithoutTags( "trigger" )
					.Run();

				if ( trace.Hit && trace.Entity.IsWorld )
				{
					var description = TypeLibrary.GetType( Type );
					var entity = description.Create<ILimitedSpawner>();
					entity.Position = trace.EndPosition;
					entity.Rotation = Rotation.Identity.RotateAroundAxis( Vector3.Up, Game.Random.Float() * 360f );
					amountToSpawn--;
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
