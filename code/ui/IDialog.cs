namespace Facepunch.Forsaken.UI;

public interface IDialog
{
	bool AllowMovement { get; }
	bool IsOpen { get; }
	void Open();
	void Close();
}
