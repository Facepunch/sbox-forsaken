using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using Conna.Inventory;
using Conna.Time;

namespace Facepunch.Forsaken;

public class LimitedSpawner
{
	public static List<LimitedSpawner> All { get; private set; } = new();

	private TimeUntil NextDespawnTime { get; set; }
	private TimeUntil NextSpawnTime { get; set; }

	public Action<ILimitedSpawner> OnSpawned { get; set; }
	public float TimeOfDayStart { get; set; } = 0f;
	public float TimeOfDayEnd { get; set; } = 0f;
	public bool SpawnNearPlayers { get; set; }
	public float MinPercentPerSpawn { get; set; } = 0f;
	public float MaxPercentPerSpawn { get; set; } = 0.5f;
	public bool MultiplyTotalByPlayers { get; set; }
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

	private float GetCalculatedTotal()
	{
		if ( MultiplyTotalByPlayers )
			return MaxTotal * Game.Clients.Count;
		else
			return MaxTotal;
	}

	[GameEvent.Tick.Server]
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
		if ( !Navigation.IsReady ) return;

		if ( !isCorrectTimePeriod )
			return;

		var totalToSpawn = GetCalculatedTotal();
		var totalCount = Entity.All
			.OfType<ILimitedSpawner>()
			.Where( p => p.GetType() == Type )
			.Count();

		var availableToSpawn = totalToSpawn - totalCount;

		if ( availableToSpawn > 0 )
		{
			var minToSpawn = Math.Min( totalToSpawn * MinPercentPerSpawn, availableToSpawn ).CeilToInt();
			var maxToSpawn = Math.Min( totalToSpawn * MaxPercentPerSpawn, availableToSpawn ).CeilToInt();
			var amountToSpawn = Game.Random.Int( minToSpawn, maxToSpawn );
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
		entity.Position = Navigation.FindNearestWalkable( position );
		entity.Rotation = Rotation.Identity.RotateAroundAxis( Vector3.Up, Game.Random.Float() * 360f );
		OnSpawned?.Invoke( entity );
	}
}
