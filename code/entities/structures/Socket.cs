using Sandbox;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Socket : Entity
{
	public struct Match
	{
		public Socket Ours { get; private set; }
		public Socket Theirs { get; private set; }
		public bool IsValid { get; private set; }

		public Match( Socket ours, Socket theirs )
		{
			Ours = ours;
			Theirs = theirs;
			IsValid = true;
		}

		public Match()
		{
			Ours = null;
			Theirs = null;
			IsValid = false;
		}
	}

	[Net] public Socket Connection { get; private set; }
	[Net] public IList<string> ConnectAny { get; set; } = new List<string>();
	[Net] public IList<string> ConnectAll { get; set; } = new List<string>();

	public PersistenceHandle Handle { get; private set; }
	private PersistenceHandle ConnectionHandle { get; set; }

	public override void Spawn()
	{
		Transmit = TransmitType.Always;
		Handle = new();

		base.Spawn();
	}

	public bool CanConnectTo( Socket socket )
	{
		foreach ( var tag in ConnectAll )
		{
			if ( !socket.Tags.Has( tag ) )
				return false;
		}

		foreach ( var tag in ConnectAny )
		{
			if ( socket.Tags.Has( tag ) )
				return true;
		}

		return false;
	}

	public void Disconnect( Socket socket )
	{
		if ( Connection == socket )
		{
			socket.Connection = null;
			Connection = null;
		}
	}

	public void Connect( Socket socket )
	{
		if ( Connection == socket ) return;

		socket.Connection = this;
		Connection = socket;
	}

	public void Serialize( BinaryWriter writer )
	{
		writer.Write( Handle );

		if ( Connection.IsValid() )
		{
			writer.Write( true );
			writer.Write( Connection.Handle );
		}
		else
		{
			writer.Write( false );
		}
	}

	public void Deserialize( BinaryReader reader )
	{
		Handle = reader.ReadPersistenceHandle();

		var hasConnection = reader.ReadBoolean();

		if ( hasConnection )
		{
			ConnectionHandle = reader.ReadPersistenceHandle();
		}
	}

	public void RestoreConnection()
	{
		if ( ConnectionHandle.IsValid() )
		{
			var socket = All.OfType<Socket>().FirstOrDefault( s => s.Handle.Equals( ConnectionHandle ) );

			if ( socket.IsValid() )
			{
				Connect( socket );
			}
		}
	}
}
