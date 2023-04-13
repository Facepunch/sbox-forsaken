using Sandbox;

namespace Facepunch.Forsaken;

public partial class RainyWeather : WeatherCondition
{
	[Net] public float Density { get; set; } = 50f;
	[Net] public Color Tint { get; set; } = Color.White;

	private Sound AmbientSound;
	private Particles InnerParticles;
	private Particles OuterParticles;

	public override void OnStarted()
	{
		if ( Game.IsClient )
		{
			InnerParticles = Particles.Create( "particles/precipitation/rain_inner.vpcf" );
			OuterParticles = Particles.Create( "particles/precipitation/rain_outer.vpcf" );
			AmbientSound = Sound.FromScreen( "sounds/ambient/rain-loop.sound" );
		}

		base.OnStarted();
	}

	public override void OnStopped()
	{
		if ( Game.IsClient )
		{
			InnerParticles?.Destroy();
			OuterParticles?.Destroy();
			AmbientSound.Stop();
		}

		base.OnStopped();
	}

	public override void ClientTick()
	{
		if ( !ForsakenPlayer.Me.IsValid() ) return;

		if ( InnerParticles != null )
		{
			InnerParticles.SetPosition( 1, ForsakenPlayer.Me.Position + Vector3.Up * 300f );
			InnerParticles.SetPosition( 3, Vector3.Forward * Density );
			InnerParticles.SetPosition( 4, Tint * 255f );
		}

		if ( OuterParticles != null )
		{
			OuterParticles.SetPosition( 1, Camera.Position );
			OuterParticles.SetPosition( 3, Vector3.Forward * Density );
			OuterParticles.SetPosition( 4, Tint * 255f );
		}
	}
}

