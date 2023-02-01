using Sandbox;
using System;

namespace Facepunch.Forsaken;

public abstract class SingleDoorItem : DeployableItem
{
	public virtual StructureMaterial Material => StructureMaterial.Wood;

	public override bool IsStructure => true;
	public override string PlaceSoundName => "door.single.placed";
	public override string Description => "A single door that can be placed in a doorway. Only you can open it unless a code lock is added.";

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
