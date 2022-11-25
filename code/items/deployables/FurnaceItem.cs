using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class FurnaceItem : DeployableItem
{
	public override Type Deployable => typeof( Furnace );
	public override string[] ValidTags => new string[] { "world", "foundation" };
	public override string Model => "models/furnace/furnace.vmdl";
	public override string Description => "A basic furnace. I can put stuff into it that I want to melt quickly.";
	public override string UniqueId => "furnace";
	public override string Icon => "textures/items/furnace.png";
	public override string Name => "Furnace";

	public override bool CanPlaceOn( Entity entity )
	{
		return entity.IsWorld || entity is Foundation;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
