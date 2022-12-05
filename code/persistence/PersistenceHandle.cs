using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Facepunch.Forsaken;

public struct PersistenceHandle : IEqualityComparer<PersistenceHandle>
{
	private ulong? InternalId { get; set; }

	public ulong Id
	{
		get
		{
			if ( !InternalId.HasValue )
			{
				InternalId = PersistenceSystem.GenerateId();
			}

			return InternalId.Value;
		}
	}

	public bool Equals( PersistenceHandle x, PersistenceHandle y )
	{
		return x.InternalId == y.InternalId;
	}

	public int GetHashCode( [DisallowNull] PersistenceHandle obj )
	{
		return obj.InternalId.GetHashCode();
	}

	public PersistenceHandle( ulong id )
	{
		InternalId = id;
	}

	public PersistenceHandle()
	{

	}
}
