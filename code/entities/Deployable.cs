using Sandbox;
using Sandbox.Component;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Deployable : ModelEntity, IDamageable, IPersistence
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

			var glow = Ghost.Components.GetOrCreate<Glow>();
			glow.Color = Color.White.WithAlpha( 0.8f );
			glow.InsideObscuredColor = Color.White.WithAlpha( 0.6f );
			glow.Width = 0.2f;

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

	public static bool IsCollidingWithWorld( ModelEntity entity )
	{
		var testPosition = entity.Position + Vector3.Up * 4f;
		var collision = Trace.Body( entity.PhysicsBody, entity.Transform.WithPosition( testPosition ), testPosition )
			.WithAnyTags( "solid", "world" )
			.Run();

		if ( collision.Hit || collision.StartedSolid )
			return true;

		var zones = All.OfType<BuildExclusionZone>()
			.Where( z => entity.PhysicsBody.CheckOverlap( z.PhysicsBody ) );

		return zones.Any();
	}

	public virtual float MaxHealth => 100f;

	public PersistenceHandle Handle { get; private set; }

	public virtual bool ShouldSaveState()
	{
		return true;
	}

	public virtual void BeforeStateLoaded()
	{

	}

	public virtual void AfterStateLoaded()
	{

	}

	public virtual void SerializeState( BinaryWriter writer )
	{
		writer.Write( Handle );
		writer.Write( Transform );
		writer.Write( Health );
	}

	public virtual void DeserializeState( BinaryReader reader )
	{
		Handle = reader.ReadPersistenceHandle();
		Transform = reader.ReadTransform();
		Health = reader.ReadSingle();

		UpdateNavBlocker();
	}

	public virtual void OnPlacedByPlayer( ForsakenPlayer player, TraceResult trace )
	{
		UpdateNavBlocker();
	}

	public override void Spawn()
	{
		Handle = new();

		base.Spawn();
	}

	protected void UpdateNavBlocker()
	{
		Game.AssertServer();
		Components.RemoveAny<NavBlocker>();
		Components.Add( new NavBlocker() );
		Event.Run( "fsk.navblocker.added", Position );
	}

	protected void RemoveNavBlocker()
	{
		Game.AssertServer();
		Components.RemoveAny<NavBlocker>();
		Event.Run( "fsk.navblocker.removed", Position );
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
		{
			RemoveNavBlocker();
		}

		base.OnDestroy();
	}
}
