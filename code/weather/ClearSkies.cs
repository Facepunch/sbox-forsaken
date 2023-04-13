using Sandbox;

namespace Facepunch.Forsaken;

public class ClearSkies : WeatherCondition
{
	private TimeUntil NextAttemptToChange { get; set; }

	public override void OnStarted()
	{
		if ( Game.IsServer )
		{
			NextAttemptToChange = Game.Random.Float( 30f, 60f );
		}

		base.OnStarted();
	}

	public override void ServerTick()
	{
		if ( NextAttemptToChange )
		{
			if ( Game.Random.Float() < 0.3f )
			{
				WeatherSystem.Change( new RainyWeather() );
			}

			NextAttemptToChange = Game.Random.Float( 30f, 60f );
		}

		base.ServerTick();
	}
}

