using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

public class LimitedSpawner
{
	public static List<LimitedSpawner> All { get; private set; } = new();

	private TimeUntil NextDespawnTime { get; set; }
	private TimeUntil NextSpawnTime { get; set; }

	public Action<ILimitedSpawner> OnSpawned { get; set; }
	public bool UseNavMesh { get; set; }
	public float TimeOfDayStart { get; set; } = 0f;
	public float TimeOfDayEnd { get; set; } = 0f;
	public bool SpawnNearPlayers { get; set; }
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
		var isCorrectTimePeriod = TimeSystem.IsTimeBetween( TimeOfDayStart, TimeOfDayEnd );

		if ( NextDespawnTime && !isCorrectTimePeriod )
		{
			var entities = Entity.All
			.OfType<ILimitedSpawner>()
			.Where( p => p.GetType() == Type );

			foreach ( var entity in entities )
			{
				entity.Despawn();
			}

			NextDespawnTime = 5f;
		}

		if ( Type is null || !NextSpawnTime ) return;
		if ( UseNavMesh && !NavMesh.IsLoaded ) return;

		if ( !isCorrectTimePeriod )
			return;

		var totalCount = Entity.All
			.OfType<ILimitedSpawner>()
			.Where( p => p.GetType() == Type )
			.Count();

		var availableToSpawn = MaxTotal - totalCount;

		if ( availableToSpawn > 0 )
		{
			var amountToSpawn = Game.Random.Int( Math.Min( MinPerSpawn, availableToSpawn ), Math.Min( MaxPerSpawn, availableToSpawn ) );
			var attemptsRemaining = 10000;
			var playerList = Entity.All
				.OfType<ForsakenPlayer>()
				.Where( p => p.LifeState == LifeState.Alive && p.Client.IsValid() )
				.ToList();

			while ( amountToSpawn > 0 && attemptsRemaining > 0 )
			{
				var origin = Origin;
				var range = Range;

				if ( SpawnNearPlayers )
				{
					var player = Game.Random.FromList( playerList );

					if ( player.IsValid() )
					{
						origin = player.Position;
						range = 1024f;
					}
				}

				var position = origin + new Vector3( Game.Random.Float( -1f, 1f ) * range, Game.Random.Float( -1f, 1f ) * range );
				var trace = Trace.Ray( position + Vector3.Up * 5000f, position + Vector3.Down * 5000f )
					.WithoutTags( "trigger" )
					.Run();

				if ( trace.Hit && trace.Entity.IsWorld )
				{
					CreateEntityAt( trace.EndPosition );
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

	private void CreateEntityAt( Vector3 position )
	{
		var description = TypeLibrary.GetType( Type );
		var entity = description.Create<ILimitedSpawner>();
		entity.Position = position;
		entity.Rotation = Rotation.Identity.RotateAroundAxis( Vector3.Up, Game.Random.Float() * 360f );

		if ( UseNavMesh )
		{
			var closest = NavMesh.GetClosestPoint( entity.Position );

			if ( closest.HasValue )
			{
				entity.Position = closest.Value;
			}
		}

		OnSpawned?.Invoke( entity );
	}
}
