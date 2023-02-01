using Sandbox;

namespace Facepunch.Forsaken;

public partial class WoodenDoor : SingleDoor
{
	public override float MaxHealth => 125f;
	public override StructureMaterial Material => StructureMaterial.Wood;

	public override string GetContextName()
	{
		if ( Health < MaxHealth * 0.9f )
			return $"Wooden Door ({Health.CeilToInt()}HP)";
		else
			return $"Wooden Door";
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/structures/wooden_door.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}
}
