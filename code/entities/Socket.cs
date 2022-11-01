using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class Socket : BaseNetworkable, IValid
{
	[Net] public Structure Owner { get; set; }
	[Net] public Transform LocalTransform { get; set; }
	[Net] public List<Structure> Structures { get; set; }

	public bool IsValid => Owner.IsValid();

	public Socket()
	{

	}

	public Socket( Structure owner )
	{
		Structures = new List<Structure>();
		Owner = owner;
	}

	public Transform GetWorldTransform()
	{
		return Owner.Transform.ToWorld( LocalTransform );
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
