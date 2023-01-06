using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Facepunch.Forsaken;

public static class PersistenceSystem
{
	public static int Version => 11;

	private static Dictionary<long, byte[]> PlayerData { get; set; } = new();
	private static ulong PersistentId { get; set; }
	private static string FileName => $"{Game.Server.MapIdent.ToLower()}.save";

	[ConCmd.Admin( "fsk.save.me" )]
	private static void SaveMe()
	{
		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer player )
		{
			Save( player );
		}
	}

	[ConCmd.Admin( "fsk.load.me" )]
	private static void LoadMe()
	{
		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer player )
		{
			Load( player );
		}
	}

	public static ulong GenerateId()
	{
		return ++PersistentId;
	}

	public static void Save( ForsakenPlayer player )
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				player.SerializeState( writer );
			}

			PlayerData[player.SteamId] = stream.ToArray();
		}
	}

	public static void Load( ForsakenPlayer player )
	{
		if ( PlayerData.TryGetValue( player.SteamId, out var data ) )
		{
			using ( var stream = new MemoryStream( data ) )
			{
				using ( var reader = new BinaryReader( stream ) )
				{
					player.DeserializeState( reader );
				}
			}
		}
	}

	[ConCmd.Admin( "fsk.save" )]
	public static void SaveAll()
	{
		using ( var stream = FileSystem.Data.OpenWrite( FileName ) )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( Version );

				InventorySystem.Serialize( writer );

				SavePlayers( writer );
				SaveEntities( writer );

				writer.Write( PersistentId );
			}
		}
	}

	[ConCmd.Admin( "fsk.load" )]
	public static void LoadAll()
	{
		if ( !FileSystem.Data.FileExists( FileName ) )
			return;

		foreach ( var p in Entity.All.OfType<IPersistence>() )
		{
			p.Delete();
		}

		using ( var stream = FileSystem.Data.OpenRead( FileName ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var version = reader.ReadInt32();

				if ( Version != version )
				{
					Log.Warning( "Unable to load a save from a different version!" );
					return;
				}

				InventorySystem.Deserialize( reader );

				LoadPlayers( reader );
				LoadEntities( reader );

				PersistentId = reader.ReadUInt64();

				foreach ( var p in Entity.All.OfType<IPersistence>() )
				{
					p.BeforeStateLoaded();
				}

				foreach ( var p in Entity.All.OfType<IPersistence>() )
				{
					p.AfterStateLoaded();
				}
			}
		}
	}

	private static void SaveEntities( BinaryWriter writer )
	{
		var entities = Entity.All
			.OfType<IPersistence>()
			.Where( e => e.ShouldSaveState() )
			.Where( e => e is not ForsakenPlayer );

		writer.Write( entities.Count() );

		foreach ( var entity in entities )
		{
			var description = TypeLibrary.GetType( entity.GetType() );
			writer.Write( description.Name );
			writer.Write( entity.SerializeState );
		}
	}

	private static void LoadEntities( BinaryReader reader )
	{
		var count = reader.ReadInt32();
		var entitiesAndData = new Dictionary<IPersistence, byte[]>();

		for ( var i = 0; i < count; i++ )
		{
			var typeName = reader.ReadString();
			var type = TypeLibrary.GetType( typeName );
			var length = reader.ReadInt32();
			var data = reader.ReadBytes( length );

			try
			{
				var entity = type.Create<IPersistence>();

				if ( entity.IsValid() )
				{
					entitiesAndData.Add( entity, data );
				}
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}
		}

		foreach ( var kv in entitiesAndData )
		{
			try
			{
				using ( var stream = new MemoryStream( kv.Value ) )
				{
					using ( reader = new BinaryReader( stream ) )
					{
						kv.Key.DeserializeState( reader );
					}
				}
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}
		}
	}

	private static void LoadPlayers( BinaryReader reader )
	{
		var players = reader.ReadInt32();

		for ( var i = 0; i < players; i++ )
		{
			var playerId = reader.ReadInt64();
			var dataLength = reader.ReadInt32();
			var playerData = reader.ReadBytes( dataLength );

			PlayerData[playerId] = playerData;

			var pawn = Entity.All.OfType<ForsakenPlayer>()
				.Where( p => p.SteamId == playerId )
				.FirstOrDefault();

			if ( !pawn.IsValid() )
			{
				pawn = new ForsakenPlayer();

				var client = Game.Clients.FirstOrDefault( c => c.SteamId == playerId );

				if ( client.IsValid() )
					pawn.MakePawnOf( client );
				else
					pawn.MakePawnOf( playerId );
			}

			Load( pawn );
		}
	}

	private static void SavePlayers( BinaryWriter writer )
	{
		var players = Game.Clients
			.Select( c => c.Pawn )
			.OfType<ForsakenPlayer>();

		foreach ( var player in players )
		{
			Save( player );
		}

		writer.Write( PlayerData.Count );

		foreach ( var kv in PlayerData )
		{
			writer.Write( kv.Key );
			writer.Write( kv.Value.Length );
			writer.Write( kv.Value );
		}
	}
}
