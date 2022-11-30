using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class WoodPickup : ResourcePickup
{
	public override string GetContextName()
	{
		return "Tree Stump";
	}

	public override void Spawn()
	{
		SetModel( "models/resources/tree_stump.vmdl" );
		base.Spawn();
	}
}
