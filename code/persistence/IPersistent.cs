using System.IO;

namespace Facepunch.Forsaken;

public interface IPersistent
{
	public bool ShouldPersist();
	public void Serialize( BinaryWriter writer );
	public void Deserialize( BinaryReader reader );
	public void PostLoaded();
	public void Delete();
}
