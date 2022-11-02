using Sandbox;

namespace Facepunch.Forsaken;

public class NetInventoryItem : BaseNetworkable, INetworkSerializer, IValid
{
	public InventoryItem Instance { get; private set; }

	public bool IsValid => Instance.IsValid();
	public uint Version { get; private set; }

	public NetInventoryItem()
	{

	}

	public NetInventoryItem( InventoryItem item )
	{
		Instance = item;
	}

	public void Read( ref NetRead read )
	{
		var version = read.Read<uint>();
		var totalBytes = read.Read<int>();
		var output = new byte[totalBytes];
		read.ReadUnmanagedArray( output );

		if ( Version == version ) return;

		Instance = InventoryItem.Deserialize( output );
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
