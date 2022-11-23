using System.Collections.Generic;

namespace Facepunch.Forsaken.UI;

public interface ITooltipProvider
{
	public string Name { get; }
	public string Description { get; }
	public IReadOnlySet<string> Tags { get; }
	public bool IsVisible { get; }
	public Color Color { get; }
	public bool HasHovered { get;  }
}
