using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class RainyWeather : WeatherCondition
{
	[Net] public float Density { get; set; } = 50f;
	[Net] public int Intensity { get; set; } = 1;
	[Net] public Color Tint { get; set; } = Color.White;

	private Sound AmbientSound;
	private List<Particles> InnerParticles { get; set; } = new();
	private List<Particles> OuterParticles { get; set; } = new();
	private TimeUntil NextAttemptToChange { get; set; }

	public override void OnStarted()
	{
		if ( Game.IsClient )
		{
			for ( var i = 0; i < Intensity; i++ )
			{
				InnerParticles.Add( Particles.Create( "particles/precipitation/rain_inner.vpcf" ) );
				OuterParticles.Add( Particles.Create( "particles/precipitation/rain_outer.vpcf" ) );
			}

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

			InnerParticles.ForEach( p => p.Destroy() );
			OuterParticles.ForEach( p => p.Destroy() );

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
		if ( !ForsakenPlayer.Me.IsValid() )
			return;

		SetScale( 1f );
	}

	private void SetScale( float scale )
	{
		InnerParticles.ForEach( p =>
		{
			p.SetPosition( 1, ForsakenPlayer.Me.Position + Vector3.Up * 300f );
			p.SetPosition( 3, Vector3.Forward * Density * scale );
			p.SetPosition( 4, Tint * 255f );
		} );

		OuterParticles.ForEach( p =>
		{
			p.SetPosition( 1, Camera.Position );
			p.SetPosition( 3, Vector3.Forward * Density * scale );
			p.SetPosition( 4, Tint * 255f );
		} );

		AmbientSound.SetVolume( scale );
	}
}

