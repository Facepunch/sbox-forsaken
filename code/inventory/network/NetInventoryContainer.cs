using Sandbox;

namespace Facepunch.Forsaken;

public class NetInventoryContainer : BaseNetworkable, INetworkSerializer, IValid
{
	public InventoryContainer Value { get; private set; }

	public bool IsValid => Value.IsValid();
	public uint Version { get; private set; }

	public NetInventoryContainer()
	{

	}

	public NetInventoryContainer( InventoryContainer container )
	{
		Value = container;
	}

	public bool Is( InventoryContainer container )
	{
		return container == Value;
	}

	public bool Is( NetInventoryContainer container )
	{
		return container == this;
	}

	public void Read( ref NetRead read )
	{
		var version = read.Read<uint>();
		var itemId = read.Read<ulong>();
		var totalBytes = read.Read<int>();
		var output = new byte[totalBytes];
		read.ReadUnmanagedArray( output );

		if ( Version == version ) return;

		var container = InventorySystem.Find( itemId );
		if ( container.IsValid() )
		{
			Value = container;
			return;
		}

		Value = InventoryContainer.Deserialize( output );
		Version = version;
	}

	public void Write( NetWrite write )
	{
		var serialized = Value.Serialize();
		write.Write( ++Version );
		write.Write( Value.InventoryId );
		write.Write( serialized.Length );
		write.Write( serialized );
	}
}
