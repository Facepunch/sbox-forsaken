namespace Facepunch.Forsaken;

public enum WeaponType
{
	None,
	Melee,
	Hitscan,
	Projectile
}

public abstract class WeaponConfig
{
	public virtual string ClassName => "";
	public virtual AmmoType AmmoType => AmmoType.Bolt;
	public virtual WeaponType Type => WeaponType.None;
	public virtual int Damage => 0;
	public virtual int Ammo => 0;
}
