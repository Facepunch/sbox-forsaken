using System;

namespace Facepunch.Forsaken;

public partial class StonePickup : ResourcePickup
{
	public override string ModelPath => "models/resources/stone_pile.vmdl";
	public override Type ItemType => typeof( StoneItem );
	public override int StackSize => 25;

	public override string GetContextName()
	{
		return "Stone Pile";
	}
}
