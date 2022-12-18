using System;

namespace Facepunch.Forsaken;

public partial class PlantFiberPickup : ResourcePickup
{
	public override string ModelPath => "models/resources/bush_dead.vmdl";
	public override Type ItemType => typeof( PlantFiberItem );
	public override int StackSize => 15;

	public override string GetContextName()
	{
		return "Plant";
	}
}
