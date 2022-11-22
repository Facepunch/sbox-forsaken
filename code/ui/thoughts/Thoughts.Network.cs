using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class Thoughts
{
	private static TimeSince LastThoughtTime { get; set; }
	private static string LastShownThought { get; set; }

	[ClientRpc]
	public static void Show( string thought, bool ignoreCooldown = false )
	{
		if ( !ignoreCooldown && LastShownThought == thought && LastThoughtTime < 3f )
		{
			return;
		}

		Instance?.AddEntry( thought );

		LastShownThought = thought;
		LastThoughtTime = 0f;
	}
}
