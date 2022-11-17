using Sandbox;

namespace Facepunch.Forsaken;

public partial class InventoryViewer : EntityComponent, IValid
{
	public bool IsValid => Entity.IsValid();

	[Net] public ulong ContainerId { get; private set; }

	/// <summary>
	/// The container that this viewer is currently viewing.
	/// </summary>
	public InventoryContainer Container => InventorySystem.Find( ContainerId );

	/// <summary>
	/// Set the container this viewer is currently viewing.
	/// </summary>
	public void SetContainer( InventoryContainer container )
	{
		ContainerId = container.InventoryId;
	}

	/// <summary>
	/// Clear the container this viewer is currently viewing.
	/// </summary>
	public void ClearContainer()
	{
		ContainerId = 0;
	}
}
