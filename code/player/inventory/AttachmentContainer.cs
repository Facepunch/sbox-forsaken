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

	protected override void OnItemGiven( ushort slot, InventoryItem item )
	{
		if ( item is AttachmentItem attachment && Parent is WeaponItem weapon )
		{
			attachment.OnAttached( weapon );
		}

		base.OnItemGiven( slot, item );
	}

	protected override void OnItemTaken( ushort slot, InventoryItem item )
	{
		base.OnItemTaken( slot, item );

		if ( !item.Parent.IsValid() || !item.Parent.Is( this ) )
		{
			if ( item is AttachmentItem attachment && Parent is WeaponItem weapon )
			{
				attachment.OnDetatched( weapon );
			}
		}
	}
}
