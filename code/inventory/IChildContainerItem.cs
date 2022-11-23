namespace Facepunch.Forsaken;

public interface IChildContainerItem
{
	public InventoryContainer ChildContainer { get; }
	public string ChildContainerName { get; }
}
