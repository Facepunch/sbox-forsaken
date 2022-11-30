using Sandbox;
using System;

namespace Facepunch.Forsaken;

public partial class TimedAction : BaseNetworkable
{
	[Net] public TimeUntil EndTime { get; private set; }
	[Net] public float Duration { get; private set; }
	[Net] public Vector3 Origin { get; private set; }
	[Net] public string Title { get; private set; }

	public Action OnFinished { get; private set; }

	public void Start( string title, Vector3 origin, float duration, Action callback )
	{
		OnFinished = callback;
		Duration = duration;
		EndTime = duration;
		Origin = origin;
		Title = title;
	}
}
