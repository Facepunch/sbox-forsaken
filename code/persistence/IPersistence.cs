using Sandbox;
using System.IO;

namespace Facepunch.Forsaken;

public interface IPersistence : IValid
{
	public bool ShouldSaveState();
	public void SerializeState( BinaryWriter writer );
	public void DeserializeState( BinaryReader reader );
	public void BeforeStateLoaded();
	public void AfterStateLoaded();
	public void Delete();
}
