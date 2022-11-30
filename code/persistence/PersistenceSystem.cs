using Sandbox;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Facepunch.Forsaken;

public static class PersistenceSystem
{
	public static int Version => 1;

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

				writer.Write( PlayerData.Count );

				foreach ( var kv in PlayerData )
				{
					writer.Write( kv.Key );
					writer.Write( kv.Value.Length );
					writer.Write( kv.Value );
				}
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

				var players = reader.ReadInt32();

				for ( var i = 0; i < players; i++ )
				{
					var playerId = reader.ReadInt64();
					var dataLength = reader.ReadInt32();
					var playerData = reader.ReadBytes( dataLength );

					PlayerData[playerId] = playerData;
				}
			}
		}
	}
}
