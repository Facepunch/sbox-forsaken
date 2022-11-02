namespace Facepunch.Forsaken;

public class ArmorItem : InventoryItem
{
	public override Color Color => ItemColors.Armor;
	public virtual float DamageMultiplier => 1f;
	public virtual ArmorSlot ArmorSlot => ArmorSlot.None;
	public virtual string SecondaryModel => string.Empty;
	public virtual string PrimaryModel => string.Empty;

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
