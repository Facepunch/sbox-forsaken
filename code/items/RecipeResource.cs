using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

[GameResource( "Recipe", "recipe", "A crafting recipe to produce an item in Forsaken.", Icon = "auto_awesome" )]
public class RecipeResource : GameResource
{
	[Property] public string Output { get; set; }
	[Property] public Dictionary<string,int> Inputs { get; set; }
}
