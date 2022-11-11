using System.Collections.Generic;

namespace Facepunch.Forsaken;

public interface IContextActions
{
	public Color GlowColor { get; }
	public float GlowWidth { get;}
	public List<ContextAction> GetContextActions();
}
