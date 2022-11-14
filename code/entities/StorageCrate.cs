using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public partial class StorageCrate : Deployable, IContextActionProvider
{
	public Color GlowColor => Color.White;
	public float GlowWidth => 0.4f;

	public string GetContextName()
	{
		return "Storage Crate";
	}

	public List<ContextAction> GetSecondaryActions()
	{
		return new List<ContextAction>()
		{
			new OpenAction( this ),
			new PickupAction( this )
		};
	}

	public ContextAction GetPrimaryAction()
	{
		var open = new OpenAction( this );
		return open;
	}

	public override void Spawn()
	{
		SetModel( "models/citizen_props/crate01.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		base.Spawn();
	}
}
