using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

[GameResource( "Consumable", "cons", "A type of consumable item for use with Forsaken.", Icon = "food-apple" )]
[ItemClass( typeof( ConsumableItem ) )]
public class ConsumableResource : LootTableResource
{
	[Property]
	public int MaxStackSize { get; set; } = 1;

	[Property]
	public int DefaultStackSize { get; set; } = 1;

	[Property, ResourceType( "sound" )]
	public string ConsumeSound { get; set; }

	[Property, ResourceType( "vpcf" )]
	public string ConsumeEffect { get; set; }

	[Property, ResourceType( "sound" )]
	public string ActivateSound { get; set; }

	[Property]
	public float ActivateDelay { get; set; }

	[Property]
	public List<ConsumableEffect> Effects { get; set; }

	[Property]
	public bool IsCookable { get; set; }

	[Property, ShowIf( nameof( IsCookable ), true )]
	public string CookedItemId { get; set; }

	[Property, ShowIf( nameof( IsCookable ), true )]
	public int CookedQuantity { get; set; }
}
