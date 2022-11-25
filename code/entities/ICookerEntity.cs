using Sandbox;

namespace Facepunch.Forsaken;

public interface ICookerEntity : IValid
{
	[ConCmd.Server( "fsk.cooker.toggle" )]
	public static void ToggleCmd( int entityId )
	{
		var cooker = Entity.FindByIndex( entityId ) as ICookerEntity;

		if ( cooker.IsValid() )
		{
			if ( cooker.Processor.IsActive )
				cooker.Processor.Stop();
			else
				cooker.Processor.Start();
		}
	}

	public CookingProcessor Processor { get; }
	public Vector3 Position { get; }
	public Rotation Rotation { get; }
	public int NetworkIdent { get; }
}
