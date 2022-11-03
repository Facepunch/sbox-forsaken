using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class CrateItem : DeployableItem
{
	public override Type Deployable => typeof( StorageCrate );
	public override bool CanPlaceOnWorld => false;
	public override bool CanPlaceOnFoundation => true;
	public override string Model => "models/citizen_props/crate01.vmdl";
	public override string Description => "A simple crate for storing stuff inside.";
	public override string Icon => "textures/items/crate.png";

	public override bool CanPlaceOn( Entity entity )
	{
		return entity is Foundation;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	public override void BuildTags( List<ItemTag> tags )
	{
		tags.Add( ItemTag.Deployable );
	}
}
