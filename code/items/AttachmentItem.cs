using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

public class AttachmentItem : ResourceItem<AttachmentResource, AttachmentItem>
{
	public override Color Color => ItemColors.Tool;
	public override ushort DefaultStackSize => 1;
	public override ushort MaxStackSize => 1;

	public virtual int AttachmentSlot => Resource?.AttachmentSlot ?? 0;

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "attachment" );

		base.BuildTags( tags );
	}
}
