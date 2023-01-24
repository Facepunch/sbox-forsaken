using Editor;
using Sandbox;

namespace Facepunch.Forsaken
{
	[Title( "Temperature Zone")]
	[Description( "Modifies the temperature of players who enter this area." )]
	[Category( "Triggers" )]
	[HammerEntity]
	public partial class TemperatureZone : BaseTrigger
	{
		[Property] public float Temperature { get; set; } = 0f;

		public override void StartTouch( Entity other )
		{
			if ( other is ForsakenPlayer player )
			{
				player.InsideZones.Add( this );
			}

			base.StartTouch( other );
		}

		public override void EndTouch( Entity other )
		{
			if ( other is ForsakenPlayer player )
			{
				player.InsideZones.Remove( this );
			}

			base.EndTouch( other );
		}
	}
}
