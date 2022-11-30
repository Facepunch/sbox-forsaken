using System.IO;

namespace Facepunch.Forsaken;

public interface IPersistent
{
	public void Serialize( BinaryWriter writer );
	public void Deserialize( BinaryReader reader );
}
