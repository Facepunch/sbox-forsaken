using Sandbox;

namespace Facepunch.Forsaken;

[GameResource( "Armor", "armor", "A piece of armor or clothing for use with Forsaken.", Icon = "checkroom" )]
[ItemClass( typeof( ArmorItem ) )]
public class ArmorResource : LootTableResource
{
	[Property]
	public float DamageMultiplier { get; set; } = 1f;

	[Property]
	public ArmorSlot ArmorSlot { get; set; } = ArmorSlot.None;

	[Property, ResourceType( "vmdl" )]
	public string SecondaryModel { get; set; }

	[Property, ResourceType( "vmdl" )]
	public string PrimaryModel { get; set; }

	[Property]
	public float TemperatureModifier { get; set; }
}
