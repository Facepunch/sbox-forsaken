using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class DeployableItem : InventoryItem
{
	public override Color Color => ItemColors.Deployable;
	public virtual Type Deployable => null;
	public virtual string Model => "models/citizen_props/crate01.vmdl";
	public virtual bool CanPlaceOnWorld => true;
	public virtual bool CanPlaceOnFoundation => false;

	public virtual bool CanPlaceOn( Entity entity )
	{
		return false;
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
