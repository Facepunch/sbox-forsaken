using Sandbox;

namespace Facepunch.Forsaken;

public partial class AnimateBrightness
{
	private TimeUntil NextKeyframeTime { get; set; }
	private float[] KeyframeValues { get; set; } = new float[] { 0.9f, 1f, 1f, 0.7f, 1f, 1f, 0.8f, 0.8f, 1.2f };
	private int KeyframeIndex { get; set; } = 0;
	private float Speed { get; set; }

	public AnimateBrightness( float[] keyframes, float speed )
	{
		KeyframeValues = keyframes;
		Speed = speed;
	}

	public float Update( float currentValue )
	{
		currentValue = currentValue.LerpTo( KeyframeValues[KeyframeIndex], Time.Delta * Speed );

		if ( NextKeyframeTime )
		{
			KeyframeIndex++;

			if ( KeyframeIndex >= KeyframeValues.Length )
				KeyframeIndex = 0;

			NextKeyframeTime = Time.Delta * Speed;
		}

		return currentValue;
	}
}
