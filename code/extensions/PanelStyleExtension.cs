using Sandbox;
using Sandbox.UI;

namespace Facepunch.Forsaken;

public static class PanelStyleExtension
{
	/// <summary>
	/// Set the background to a linear gradient.
	/// </summary>
	public static void SetLinearGradientBackground( this PanelStyle self, Color from, float fromOpacity, Color to, float toOpacity )
	{
		self.Set( "background", $"linear-gradient(rgba({from.Hex}, {fromOpacity}), rgba({to.Hex}, {toOpacity}))" );
	}

	/// <summary>
	/// Set the background to a radial gradient.
	/// </summary>
	public static void SetRadialGradientBackground( this PanelStyle self, Color from, float fromOpacity, Color to, float toOpacity )
	{
		self.Set( "background", $"radial-gradient(rgba({from.Hex}, {fromOpacity}), rgba({to.Hex}, {toOpacity}))" );
	}
}
