using System;

namespace Facepunch.Forsaken;

public partial class PlantFiberPickup : ResourcePickup
{
	public override string GatherSound => "rummage.plant";
	public override string ModelPath => "models/sbox_props/shrubs/pine/pine_bush_regular_c.vmdl";
	public override Type ItemType => typeof( PlantFiberItem );
	public override int StackSize => 15;

	public override string GetContextName()
	{
		return "Plant";
	}
}
