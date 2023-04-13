using Sandbox;

namespace Facepunch.Forsaken;

public partial class RainyWeather : WeatherCondition
{
	[Net] public float Density { get; set; } = 50f;
	[Net] public Color Tint { get; set; } = Color.White;

	private Sound AmbientSound;
	private Particles InnerParticles;
	private Particles OuterParticles;
	private TimeUntil NextAttemptToChange { get; set; }

	public override void OnStarted()
	{
		if ( Game.IsClient )
		{
			InnerParticles = Particles.Create( "particles/precipitation/rain_inner.vpcf" );
			OuterParticles = Particles.Create( "particles/precipitation/rain_outer.vpcf" );
			AmbientSound = Sound.FromScreen( "sounds/ambient/rain-loop.sound" );
		}
		else
		{
			NextAttemptToChange = Game.Random.Float( 30f, 60f );
		}

		base.OnStarted();
	}

	public override async void OnStopped()
	{
		if ( Game.IsClient )
		{
			var fadeOutTime = 5f;
			var scale = 1f;

			while ( scale > 0f )
			{
				await GameTask.DelaySeconds( Time.Delta );
				scale -= (Time.Delta / fadeOutTime);
				SetScale( scale );
			}

			InnerParticles?.Destroy();
			OuterParticles?.Destroy();
			AmbientSound.Stop();
		}

		base.OnStopped();
	}

	public override void ServerTick()
	{
		if ( NextAttemptToChange )
		{
			if ( Game.Random.Float() < 0.3f )
			{
				WeatherSystem.Change( new ClearSkies() );
			}

			NextAttemptToChange = Game.Random.Float( 30f, 60f );
		}

		base.ServerTick();
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

	private void SetScale( float scale )
	{
		if ( InnerParticles != null )
		{
			InnerParticles.SetPosition( 1, ForsakenPlayer.Me.Position + Vector3.Up * 300f );
			InnerParticles.SetPosition( 3, Vector3.Forward * Density * scale );
			InnerParticles.SetPosition( 4, Tint * 255f );
		}

		if ( OuterParticles != null )
		{
			OuterParticles.SetPosition( 1, Camera.Position );
			OuterParticles.SetPosition( 3, Vector3.Forward * Density * scale );
			OuterParticles.SetPosition( 4, Tint * 255f );
		}

		AmbientSound.SetVolume( scale );
	}
}

