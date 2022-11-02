namespace Facepunch.Forsaken;

public class BaseballCapItem : ArmorItem
{
	public override float DamageMultiplier => 0.9f;
	public override ArmorSlot ArmorSlot => ArmorSlot.Head;
	public override string PrimaryModel => "models/citizen_clothes/hat/balaclava/models/balaclava.vmdl";
	public override string Description => "A stylish cap. It doesn't do much.";
	public override string Name => "Baseball Cap";
	public override string Icon => "textures/items/baseball_cap.png";
}
