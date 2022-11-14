using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public interface IContextActionProvider : IValid
{
	public Color GlowColor { get; }
	public float GlowWidth { get;}
	public List<ContextAction> GetSecondaryActions();
	public int NetworkIdent { get; }
	public ContextAction GetPrimaryAction();
	public string GetContextName();
}
