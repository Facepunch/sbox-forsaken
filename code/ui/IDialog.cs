namespace Facepunch.Forsaken.UI;

public interface IDialog
{
	bool IsOpen { get; }
	void Open();
	void Close();
}
