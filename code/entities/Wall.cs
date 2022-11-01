using Sandbox;

namespace Facepunch.Forsaken;

[Title( "Wall" )]
[Description( "Prevents anything getting in or out. Must be attached to a foundation." )]
public partial class Wall : Structure
{
	public override void Spawn()
	{
		SetModel( "models/structures/wall.vmdl" );

		base.Spawn();
	}
}
