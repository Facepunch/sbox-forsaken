using Sandbox;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class AttachmentContainer : InventoryContainer
{
	public AttachmentContainer() : base()
	{
		SetSlotLimit( 4 );
	}

	public override bool CanGiveItem( ushort slot, InventoryItem item )
	{
		if ( item is not AttachmentItem attachment )
			return false;

		var existing = FindItems<AttachmentItem>()
			.Where( i => !i.Equals( item ) )
			.Where( i => i.AttachmentSlot == attachment.AttachmentSlot );

		return !existing.Any();
	}
}
