using System;

namespace Facepunch.Forsaken;

[Flags]
public enum RecipeCategory
{
	None = 0,
	Building = 1,
	Clothing = 2,
	Weapons = 4,
	Tools = 8
}
