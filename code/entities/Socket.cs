using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class Socket : Entity
{
	[Net] public IList<Structure> Structures { get; set; } = new List<Structure>();

	public override void Spawn()
	{
		Transmit = TransmitType.Always;
		base.Spawn();
	}

	public void Remove( Structure structure )
	{
		if ( structure.Socket == this )
		{
			Structures.Remove( structure );
			structure.Socket = null;
		}
	}

	public void Add( Structure structure )
	{
		if ( structure.Socket == this ) return;

		Structures.Add( structure );

		structure.Socket?.Remove( structure );
		structure.Socket = this;
	}
}
