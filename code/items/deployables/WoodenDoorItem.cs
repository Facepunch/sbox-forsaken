using Sandbox;
using System;

namespace Facepunch.Forsaken;

public class WoodenDoorItem : SingleDoorItem
{
	public override Type Deployable => typeof( WoodenDoor );
	public override string UniqueId => "wooden_door";
	public override string Icon => "textures/items/wooden_door.png";
	public override string Name => "Wooden Door";
}
