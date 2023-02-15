using Sandbox;

namespace Facepunch.Forsaken;

public static class ForsakenEvent
{
	public class NavBlockerAdded : EventAttribute
	{
		public NavBlockerAdded() : base( "fsk.navblocker.added" )
		{

		}
	}

	public class NavBlockerRemoved : EventAttribute
	{
		public NavBlockerRemoved() : base( "fsk.navblocker.removed" )
		{

		}
	}
}
