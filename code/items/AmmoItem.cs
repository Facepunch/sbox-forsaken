using System.IO;

namespace Facepunch.Forsaken;

public class AmmoItem : ResourceItem<AmmoResource, AmmoItem>
{
	public override Color Color => ItemColors.Ammo;
	public override ushort DefaultStackSize => (ushort)(Resource?.DefaultStackSize ?? 1);
	public override ushort MaxStackSize => (ushort)(Resource?.MaxStackSize ?? 1);
	public virtual AmmoType AmmoType => Resource?.AmmoType ?? AmmoType.None;

	public override bool CanStackWith( InventoryItem other )
	{
		return (other is AmmoItem item && item.AmmoType == AmmoType);
	}
}
