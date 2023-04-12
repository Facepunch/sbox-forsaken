using Sandbox;
using System;

namespace Facepunch.Forsaken;

public partial class MetalDoor : SingleDoor
{
	public override float MaxHealth => 500f;
	public override Type ItemType => typeof( MetalDoorItem );
	public override StructureMaterial Material => StructureMaterial.Metal;

	public override string GetContextName()
	{
		if ( Health < MaxHealth * 0.9f )
			return $"Metal Door ({Health.CeilToInt()}HP)";
		else
			return $"Metal Door";
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/metal_door.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}
}
