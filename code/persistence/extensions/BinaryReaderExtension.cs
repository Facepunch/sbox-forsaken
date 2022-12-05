using System.IO;

namespace Facepunch.Forsaken;

public static partial class BinaryReaderExtension
{
	public static PersistenceHandle ReadPersistenceHandle( this BinaryReader buffer )
	{
		var id = buffer.ReadUInt64();
		return new PersistenceHandle( id );
	}
}
