using Sandbox;

namespace Facepunch.Forsaken;

public partial class WeatherSystem : Entity
{
	public delegate void WeatherChanged( WeatherCondition oldCondition, WeatherCondition newCondition );
	public static event WeatherChanged OnWeatherChanged;

	public static WeatherSystem Instance { get; private set; }
	public static WeatherCondition Condition => Instance?.InternalCondition;

	[Net, Change( nameof( OnInternalConditionChanged ) )]
	private WeatherCondition InternalCondition { get; set; }

	private TimeUntil NextChangeWeather;

	[Event.Entity.PostSpawn]
	private static void Initialize()
	{
		Game.AssertServer();
		Instance = new WeatherSystem();
		Change( new ClearSkies() );
	}

	public static void Change( WeatherCondition condition )
	{
		Game.AssertServer();

		if ( !Instance.IsValid() )
			return;

		var oldCondition = Instance.InternalCondition;

		Instance.InternalCondition = condition;
		Instance.OnInternalConditionChanged( oldCondition, condition );
	}

	public override void ClientSpawn()
	{
		Instance = this;
		base.ClientSpawn();
	}

	public override void Spawn()
	{
		Transmit = TransmitType.Always;
		base.Spawn();
	}

	[Event.Tick.Server]
	private void ServerTick()
	{
		if ( NextChangeWeather )
		{
			if ( Condition is RainyWeather )
				Change( null );
			else
				Change( new RainyWeather() );

			NextChangeWeather = Game.Random.Float( 4f, 8f );
		}

		Condition?.ServerTick();
	}

	[Event.Tick.Client]
	private void ClientTick()
	{
		Condition?.ClientTick();
	}

	private void OnInternalConditionChanged( WeatherCondition oldCondition, WeatherCondition newCondition )
	{
		oldCondition?.OnStopped();
		newCondition?.OnStarted();

		OnWeatherChanged?.Invoke( oldCondition, newCondition );
	}
}
