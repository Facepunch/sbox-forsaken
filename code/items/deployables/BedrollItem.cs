using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class BedrollItem : DeployableItem
{
	public override Type Deployable => typeof( Bedroll );
	public override string[] ValidTags => new string[] { "world", "foundation" };
	public override string Model => "models/bedroll/bedroll.vmdl";
	public override string Description => "A simple bedroll made from plant fiber. It provides warmth, and can be used as a respawn point.";
	public override string UniqueId => "bedroll";
	public override string Icon => "textures/items/bedroll.png";
	public override string Name => "Bedroll";

	public override bool CanPlaceOn( Entity entity )
	{
		return entity.IsWorld || entity is Foundation;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
