using Sandbox;

namespace Facepunch.Forsaken;

public static class ParticlesExtension
{
	/// <summary>
	/// Automatically destroy a particle effect after a time.
	/// </summary>
	public static async void AutoDestroy( this Particles self, float delay )
	{
		await GameTask.DelaySeconds( delay );
		self.Destroy();
	}
}
