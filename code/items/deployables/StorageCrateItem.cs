using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class StorageCrateItem : DeployableItem
{
	public override Type Deployable => typeof( StorageCrate );
	public override string[] ValidTags => new string[] { "world", "foundation" };
	public override string Model => "models/citizen_props/crate01.vmdl";
	public override string Description => "A simple crate for storing stuff inside.";
	public override string UniqueId => "storage_crate";
	public override string Icon => "textures/items/crate.png";
	public override string Name => "Storage Crate";

	public override bool CanPlaceOn( Entity entity )
	{
		return entity is Foundation;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
