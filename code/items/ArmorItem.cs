namespace Facepunch.Forsaken;

public class ArmorItem : ResourceItem<ArmorResource, ArmorItem>
{
	public override Color Color => ItemColors.Armor;
	public virtual float DamageMultiplier => Resource.DamageMultiplier;
	public virtual ArmorSlot ArmorSlot => Resource.ArmorSlot;
	public virtual string SecondaryModel => Resource.SecondaryModel;
	public virtual string PrimaryModel => Resource.PrimaryModel;

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
