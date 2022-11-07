namespace Facepunch.Forsaken;

public class ArmorItem : ResourceItem<ArmorResource, ArmorItem>
{
	public override Color Color => ItemColors.Armor;
	public virtual float DamageMultiplier => Resource?.DamageMultiplier ?? 1f;
	public virtual ArmorSlot ArmorSlot => Resource?.ArmorSlot ?? ArmorSlot.None;
	public virtual string SecondaryModel => Resource?.SecondaryModel ?? string.Empty;
	public virtual string PrimaryModel => Resource?.PrimaryModel ?? string.Empty;

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
