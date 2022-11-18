using Sandbox;

namespace Facepunch.Forsaken;

public partial class CraftingQueueEntry : BaseNetworkable
{
	[Net] public TimeUntil FinishTime { get; set; }
	[Net] public int ResourceId { get; set; }
	[Net] public int Quantity { get; set; }

	public RecipeResource Recipe => ResourceLibrary.Get<RecipeResource>( ResourceId );
}
