using Sandbox;

namespace Facepunch.Forsaken;

[GameResource( "Attachment", "atchmnt", "A type of attachment for weapons in Forsaken.", Icon = "attachment" )]
[ItemClass( typeof( AttachmentItem ) )]
public class AttachmentResource : ItemResource
{
	[Property, Range( 0f, 5f )]
	public int AttachmentSlot => 0;
}
