using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class ItemResource : GameResource
{
	[Property]
	public string ItemName { get; set; }

	[Property]
	public string Description { get; set; }

	[Property, ResourceType( "png" )]
	public string Icon { get; set; }

	[Property, ResourceType( "vmdl" )]
	public string WorldModel { get; set; } = "models/sbox_props/burger_box/burger_box.vmdl";

	[Property]
	public bool IsCraftable { get; set; }

	[Property, ShowIf( nameof( IsCraftable ), true )]
	public Dictionary<string,int> RequiredItems { get; set; }
}
