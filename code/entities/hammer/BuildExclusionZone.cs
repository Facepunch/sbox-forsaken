using Editor;
using Sandbox;

namespace Facepunch.Forsaken
{
	[Title( "Build Exclusion Zone")]
	[Description( "Prevent players from building in this area." )]
	[Category( "Triggers" )]
	[HammerEntity]
	public partial class BuildExclusionZone : BaseTrigger
	{
		public override void Spawn()
		{
			base.Spawn();

			EnableTraceAndQueries = true;
			Transmit = TransmitType.Always;

			Tags.Add( "nobuild" );
		}
	}
}
