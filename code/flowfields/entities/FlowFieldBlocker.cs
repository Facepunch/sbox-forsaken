using Sandbox;
using Editor;

namespace Facepunch.Forsaken.FlowFields.Entities
{
	/// <summary>
	/// Blocks off areas that players should not be able to access.
	/// </summary>
	[Library( "ff_blocker" )]
	[AutoApplyMaterial( "materials/hammer/flowfield_blocker.vmat" )]
	[Title( "Flow Field Blocker" )]
	[Solid]
	public class FlowFieldBlocker : ModelEntity
	{
		public override void Spawn()
		{
			base.Spawn();

			SetupPhysicsFromModel( PhysicsMotionType.Keyframed, true );
			Tags.Add( "playerclip" );
			Transmit = TransmitType.Never;
		}
	}
}
