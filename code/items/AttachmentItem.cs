using Sandbox;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class AttachmentItem : InventoryItem
{
	public override Color Color => ItemColors.Tool;
	public override ushort DefaultStackSize => 1;
	public override ushort MaxStackSize => 1;

	public virtual int AttachmentSlot => 0;

	public WeaponItem AttachedTo => Parent?.Parent as WeaponItem;

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "attachment" );

		base.BuildTags( tags );
	}

	public virtual void OnWeaponChanged( Weapon weapon )
	{

	}

	public virtual void OnAttached( WeaponItem item )
	{

	}

	public virtual void OnDetatched( WeaponItem item )
	{

	}

	public virtual void Simulate( IClient client )
	{

	}

	public override void OnRemoved()
	{
		if ( AttachedTo.IsValid() )
		{
			OnDetatched( AttachedTo );
		}

		base.OnRemoved();
	}
}
