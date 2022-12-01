using Sandbox;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Facepunch.Forsaken;

public static class PersistenceSystem
{
	public static int Version => 3;

	private static Dictionary<long, byte[]> PlayerData { get; set; } = new();

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

	public static void Save( ForsakenPlayer player )
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				player.Serialize( writer );
			}

			PlayerData[player.Client.PlayerId] = stream.ToArray();
		}
	}

	public static void Load( ForsakenPlayer player )
	{
		if ( PlayerData.TryGetValue( player.Client.PlayerId, out var data ) )
		{
			using ( var stream = new MemoryStream( data ) )
			{
				using ( var reader = new BinaryReader( stream ) )
				{
					player.Deserialize( reader );
				}
			}
		}
	}

	[ConCmd.Admin( "fsk.save" )]
	public static void SaveAll()
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( Version );

				InventorySystem.Serialize( writer );

				SavePlayers( writer );
				SaveEntities( writer );
			}

			FileSystem.Data.WriteAllText( "world.save", Encoding.Unicode.GetString( stream.ToArray() ) );
		}
	}

	[ConCmd.Admin( "fsk.load" )]
	public static void LoadAll()
	{
		if ( !FileSystem.Data.FileExists( "world.save" ) )
			return;

		var data = Encoding.Unicode.GetBytes( FileSystem.Data.ReadAllText( "world.save" ) );

		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var version = reader.ReadInt32();

				if ( Version != version )
				{
					Log.Error( "Unable to load a save from a different version!" );
					return;
				}

				InventorySystem.Deserialize( reader );

				LoadPlayers( reader );
				LoadEntities( reader );
			}
		}
	}

	private static void SaveEntities( BinaryWriter writer )
	{
		var entities = Entity.All
			.OfType<IPersistent>()
			.Where( e => e is not ForsakenPlayer );

		writer.Write( entities.Count() );

		foreach ( var entity in entities )
		{
			var description = TypeLibrary.GetDescription( entity.GetType() );
			writer.Write( description.Name );
			writer.WriteWrapped( entity.Serialize );
		}
	}

	private static void LoadEntities( BinaryReader reader )
	{
		var count = reader.ReadInt32();

		for ( var i = 0; i < count; i++ )
		{
			var typeName = reader.ReadString();
			var type = TypeLibrary.GetDescription( typeName );

			reader.ReadWrapped( r =>
			{
				var entity = type.Create<Entity>();

				if ( entity is IPersistent i )
				{
					i.Deserialize( r );
				}
			} );
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
		}
	}

	private static void SavePlayers( BinaryWriter writer )
	{
		var players = Client.All
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
