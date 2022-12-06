using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class SingleDoorItem : DeployableItem
{
	public override Type Deployable => typeof( SingleDoor );
	public override bool IsStructure => true;
	public override string Description => "A single door that can be placed in a doorway.";
	public override string UniqueId => "single_door";
	public override string Icon => "textures/items/furnace.png";
	public override string Name => "Single Door";

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
