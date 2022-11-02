using Sandbox;

namespace Facepunch.Forsaken;

public class NetInventoryContainer : BaseNetworkable, INetworkSerializer, IValid
{
	public InventoryContainer Instance { get; private set; }

	public bool IsValid => Instance.IsValid();
	public uint Version { get; private set; }

	public NetInventoryContainer()
	{

	}

	public NetInventoryContainer( InventoryContainer container )
	{
		Instance = container;
	}

	public bool Is( InventoryContainer container )
	{
		return container == Instance;
	}

	public bool Is( NetInventoryContainer container )
	{
		return container == this;
	}

	public void Read( ref NetRead read )
	{
		var version = read.Read<uint>();
		var totalBytes = read.Read<int>();
		var output = new byte[totalBytes];
		read.ReadUnmanagedArray( output );

		if ( Version == version ) return;

		Instance = InventoryContainer.Deserialize( output );
		Version = version;
	}

	public void Write( NetWrite write )
	{
		var serialized = Instance.Serialize();
		write.Write( ++Version );
		write.Write( serialized.Length );
		write.Write( serialized );
	}
}
