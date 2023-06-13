using Sandbox;
using System.Linq;
using Conna.Inventory;

namespace Facepunch.Forsaken;

public partial class RecyclerInputContainer : InventoryContainer
{
	public RecyclerInputContainer() : base()
	{
		SetSlotLimit( 6 );
	}

	public override bool CanGiveItem( ushort slot, IInventoryItem item )
	{
		if ( item is not IRecyclableItem recyclable )
			return false;

		if ( (recyclable.RecycleOutput?.Count ?? 0) == 0 )
		{
			var recipe = ResourceLibrary.GetAll<RecipeResource>()
				.FirstOrDefault( r => r.Output.ToLower() == item.UniqueId.ToLower() );

			if ( recipe == null )
				return false;
		}

		return recyclable.IsRecyclable;
	}
}
