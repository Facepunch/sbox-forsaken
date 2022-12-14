using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class InventoryViewer : EntityComponent, IValid
{
	public bool IsValid => Entity.IsValid();

	[Net] public IList<ulong> ContainerIds { get; private set; } = new List<ulong>();

	/// <summary>
	/// The container that this viewer is currently viewing.
	/// </summary>
	public IEnumerable<InventoryContainer> Containers
	{
		get
		{
			foreach ( var id in ContainerIds )
			{
				yield return InventorySystem.Find( id );
			}
		}
	}

	/// <summary>
	/// Set the container this viewer is currently viewing.
	/// </summary>
	public void AddContainer( InventoryContainer container )
	{
		if ( !ContainerIds.Contains( container.InventoryId ) )
		{
			ContainerIds.Add( container.InventoryId );
		}
	}

	/// <summary>
	/// Clear the container this viewer is currently viewing.
	/// </summary>
	public void ClearContainers()
	{
		ContainerIds.Clear();
	}
}
