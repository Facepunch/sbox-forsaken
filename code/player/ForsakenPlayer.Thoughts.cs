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

	private static string[] HungryThoughts = new string[]
	{
		"I could do with eating something soon.",
		"I'm feeling a little peckish...",
		"Some food would be nice."
	};

	private static string[] ThirstyThoughts = new string[]
	{
		"I'm feeling a little thirsty.",
		"I should try to find a drink soon.",
		"I'm a little thirsty."
	};

	private static string[] StarvingThoughts = new string[]
	{
		"I'm extremely hungry!",
		"I'm dying of hunger!",
		"I need to eat something, fast!"
	};

	private static string[] DehydrationThoughts = new string[]
	{
		"I'm extremely dehydrated!",
		"I need to drink something, fast!",
		"I need water... I'm dying!"
	};
}
