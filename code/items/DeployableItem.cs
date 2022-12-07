using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class DeployableItem : InventoryItem
{
	public override Color Color => ItemColors.Deployable;
	public virtual Type Deployable => null;
	public virtual string PlaceSoundName => "deployable.place";
	public virtual string Model => "models/citizen_props/crate01.vmdl";
	public virtual string[] ValidTags => new string[] { "world" };
	public virtual bool IsStructure => false;

	public virtual bool CanPlaceOn( Entity entity )
	{
		return false;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "deployable" );

		base.BuildTags( tags );
	}
}
