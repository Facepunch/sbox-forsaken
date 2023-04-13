using Sandbox;

namespace Facepunch.Forsaken;

public static class Gore
{
	public static void RegularImpact( Vector3 position, Vector3 force )
	{
		var particles = Particles.Create( "particles/gameplay/player/taken_damage/taken_damage.vpcf", position );
		particles.SetForward( 0, force.Normal );
	}

	public static void Gib( Vector3 position )
	{
		var particles = Particles.Create( "particles/blood/explosion_blood/explosion_blood.vpcf", position );
		particles.SetForward( 0, Vector3.Up );

		particles = Particles.Create( "particles/blood/gib.vpcf", position );
		particles.SetForward( 0, Vector3.Down );
	}
}
