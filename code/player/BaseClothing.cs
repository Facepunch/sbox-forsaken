using Sandbox;

namespace Facepunch.Forsaken;

public partial class BaseClothing : ModelEntity
{
	public ForsakenPlayer Wearer => Parent as ForsakenPlayer;

	public virtual void Attached() { }

	public virtual void Detatched() { }
}
