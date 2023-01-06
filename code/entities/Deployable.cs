using Sandbox;

namespace Facepunch.Forsaken;

public partial class Deployable : ModelEntity
{
	public static ModelEntity Ghost { get; private set; }

	public static ModelEntity GetOrCreateGhost( Model model )
	{
		if ( !Ghost.IsValid() || Ghost.Model != model )
		{
			ClearGhost();

			Ghost = new ModelEntity
			{
				EnableShadowCasting = false,
				EnableShadowReceive = false,
				EnableAllCollisions = false,
				Transmit = TransmitType.Never,
				Model = model
			};

			Ghost.SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
			Ghost.SetMaterialOverride( Material.Load( "materials/blueprint.vmat" ) );
		}

		return Ghost;
	}

	public static void ClearGhost()
	{
		Ghost?.Delete();
		Ghost = null;
	}

	public virtual void OnPlacedByPlayer( ForsakenPlayer player, TraceResult trace )
	{

	}
}
