using Sandbox;
using System.IO;

namespace Facepunch.Forsaken;

public interface IPersistence : IValid
{
	public bool ShouldSave();
	public void Serialize( BinaryWriter writer );
	public void Deserialize( BinaryReader reader );
	public void OnLoaded();
	public void Delete();
}
