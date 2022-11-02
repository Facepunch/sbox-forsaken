using Sandbox;

namespace Facepunch.Forsaken;

public partial class BaseClothing : ModelEntity
{
	public Player Wearer => Parent as Player;

	public virtual void Attached() { }

	public virtual void Detatched() { }
}
