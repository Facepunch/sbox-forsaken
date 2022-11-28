using Sandbox;

namespace Facepunch.Forsaken;

[GameResource( "Ammo", "ammo", "A type of weapon ammunition for use with Forsaken.", Icon = "bento" )]
[ItemClass( typeof( AmmoItem ) )]
public class AmmoResource : LootTableResource
{
	[Property]
	public AmmoType AmmoType { get; set; } = AmmoType.None;

	[Property]
	public int MaxStackSize { get; set; } = 1;

	[Property]
	public int DefaultStackSize { get; set; } = 1;
}
