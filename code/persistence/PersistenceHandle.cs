using Sandbox;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Facepunch.Forsaken;

public class PersistenceHandle : IEqualityComparer<PersistenceHandle>, IValid
{
	public bool IsValid => InternalId.HasValue;

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

	public PersistenceHandle( ulong id )
	{
		InternalId = id;
	}

	public PersistenceHandle()
	{

	}

	public PersistenceHandle Generate()
	{
		if ( !InternalId.HasValue )
		{
			InternalId = PersistenceSystem.GenerateId();
		}

		return this;
	}

	#region Equality
	public static bool operator ==( PersistenceHandle a, PersistenceHandle b )
	{
		if ( (object)a == null )
			return (object)b == null;
		else
			return a.Equals( b );
	}

	public static bool operator !=( PersistenceHandle a, PersistenceHandle b )
	{
		return !(a == b);
	}

	public bool Equals( PersistenceHandle x, PersistenceHandle y )
	{
		return x.InternalId == y.InternalId;
	}

	public int GetHashCode( [DisallowNull] PersistenceHandle obj )
	{
		return obj.InternalId.GetHashCode();
	}

	public override int GetHashCode()
	{
		return InternalId.GetHashCode();
	}

	public override bool Equals( object obj )
	{
		if ( ReferenceEquals( this, obj ) )
		{
			return true;
		}

		if ( ReferenceEquals( obj, null ) )
		{
			return false;
		}

		if ( obj is PersistenceHandle b )
		{
			return InternalId == b.InternalId;
		}

		return false;
	}
	#endregion
}
