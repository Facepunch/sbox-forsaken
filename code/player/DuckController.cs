using Sandbox;

namespace Facepunch.Forsaken;

public class DuckController
{
	public MoveController Controller { get; private set; }
	public bool IsActive { get; private set; }

	private Vector3 OriginalMins { get; set; }
	private Vector3 OriginalMaxs { get; set; }

	public DuckController( MoveController controller )
	{
		Controller = controller;
	}

	public void PreTick() 
	{
		var doesWantToDuck = Input.Down( InputButton.Duck );

		if ( doesWantToDuck != IsActive ) 
		{
			if ( doesWantToDuck )
				TryDuck();
			else
				TryUnDuck();
		}

		if ( IsActive )
		{
			Controller.SetTag( "ducked" );
			Controller.Player.EyeLocalPosition *= 0.8f;
		}
	}

	protected void TryDuck()
	{
		IsActive = true;
	}

	protected void TryUnDuck()
	{
		var pm = Controller.TraceBBox( Controller.Player.Position, Controller.Player.Position, OriginalMins, OriginalMaxs );
		if ( pm.StartedSolid ) return;
		IsActive = false;
	}

	public void UpdateBBox( ref Vector3 mins, ref Vector3 maxs, float scale )
	{
		OriginalMins = mins;
		OriginalMaxs = maxs;

		if ( IsActive )
			maxs = maxs.WithZ( 36 * scale );
	}

	public float GetWishSpeed()
	{
		if ( !IsActive ) return -1f;
		return 97f;
	}
}
