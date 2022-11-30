using Sandbox;
using System;

namespace Facepunch.Forsaken;

public struct TimedActionInfo
{
	public Action<ForsakenPlayer> OnFinished { get; private set; }
	public float Duration { get; set; }
	public Vector3 Origin { get; set; }
	public string Title { get; set; }
	public string Icon { get; set; }

	public TimedActionInfo( Action<ForsakenPlayer> callback )
	{
		OnFinished = callback;
		Duration = default;
		Origin = default;
		Title = default;
		Icon = default;
	}
}

public partial class TimedAction : BaseNetworkable
{
	[Net] public RealTimeUntil EndTime { get; private set; }
	[Net] public float Duration { get; private set; }
	[Net] public Vector3 Origin { get; private set; }
	[Net] public string Title { get; private set; }
	[Net] public string Icon { get; private set; }

	public Action<ForsakenPlayer> OnFinished { get; private set; }

	public TimedAction()
	{

	}

	public TimedAction( TimedActionInfo info )
	{
		OnFinished = info.OnFinished;
		Duration = info.Duration;
		EndTime = info.Duration;
		Origin = info.Origin;
		Title = info.Title;
		Icon = info.Icon;
	}
}
