namespace Facepunch.Forsaken;

public partial class ForsakenPlayer
{
	private static string[] InvalidPlacementThoughts = new string[]
	{
		"It won't fit here, I should try elsewhere.",
		"Hmm... this doesn't go there.",
		"I can't place that here.",
		"It doesn't seem to go there.",
		"I should try to place it somewhere else."
	};

	private static string[] MissingItemsThoughts = new string[]
	{
		"I don't have the required items to do that.",
		"I seem to be missing some items for that.",
		"I don't have enough to do that."
	};

	private static string[] UnauthorizedThoughts = new string[]
	{
		"I don't have permission to do that here.",
		"I'm not authorized to do that here."
	};

	private static string[] OutOfSightThoughts = new string[]
	{
		"That's too far out of my sight.",
		"I can't see over there.",
		"That's just out of my view."
	};

	private static string[] OutOfRangeThoughts = new string[]
	{
		"I can't reach that location.",
		"That's too far away.",
		"I should try getting closer."
	};
}
