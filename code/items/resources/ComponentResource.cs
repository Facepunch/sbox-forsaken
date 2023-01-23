using Sandbox;

namespace Facepunch.Forsaken;

[GameResource( "Component", "comp", "A type of crafting component for use with Forsaken.", Icon = "build_circle" )]
[ItemClass( typeof( ComponentItem ) )]
public class ComponentResource : ForsakenItemResource
{
	[Property]
	public int MaxStackSize { get; set; } = 5;

	[Property]
	public int DefaultStackSize { get; set; } = 1;
}
