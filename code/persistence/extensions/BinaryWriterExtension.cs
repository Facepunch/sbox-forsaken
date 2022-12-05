using System.IO;

namespace Facepunch.Forsaken;

public static partial class BinaryWriterExtension
{
	public static void Write( this BinaryWriter self, PersistenceHandle item )
	{
		self.Write( item.Id );
	}
}
