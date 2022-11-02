using Sandbox;
using System.Collections.Generic;

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

	public override void Spawn()
	{
		Transmit = TransmitType.Always;
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
}
