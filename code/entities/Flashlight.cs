using Sandbox;

namespace Facepunch.Forsaken
{
	public partial class Flashlight : SpotLightEntity
	{
		public Flashlight() : base()
		{
			Transmit = TransmitType.Always;
			InnerConeAngle = 10f;
			OuterConeAngle = 20f;
			Brightness = 1.2f;
			QuadraticAttenuation = 1f;
			LinearAttenuation = 0f;
			Color = new Color( 0.9f, 0.87f, 0.6f );
			Falloff = 4f;
			Enabled = true;
			DynamicShadows = true;
			Range = 800f;
		}
	}
}
