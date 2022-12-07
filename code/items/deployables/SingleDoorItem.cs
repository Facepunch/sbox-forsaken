using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class SingleDoorItem : DeployableItem
{
	public override Type Deployable => typeof( SingleDoor );
	public override bool IsStructure => true;
	public override string PlaceSoundName => "door.single.placed";
	public override string Description => "A single door that can be placed in a doorway. Only you can open it unless a code lock is added.";
	public override string UniqueId => "single_door";
	public override string Icon => "textures/items/single_door.png";
	public override string Name => "Single Door";

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
