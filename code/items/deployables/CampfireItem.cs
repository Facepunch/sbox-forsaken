using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class CampfireItem : DeployableItem
{
	public override Type Deployable => typeof( Campfire );
	public override string[] ValidTags => new string[] { "world", "foundation" };
	public override string Model => "models/campfire/campfire.vmdl";
	public override string Description => "A simple campfire made from stones. It should keep me warm.";
	public override string UniqueId => "campfire";
	public override string Icon => "textures/items/campfire.png";
	public override string Name => "Campfire";

	public override bool CanPlaceOn( Entity entity )
	{
		return entity.IsWorld || entity is Foundation;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
