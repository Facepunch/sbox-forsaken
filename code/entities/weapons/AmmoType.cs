namespace Facepunch.Forsaken;

public enum AmmoType
{
	None = 0,
	Pistol = 1,
	SMG = 2,
	Rifle = 3,
	Bolt = 4
}

public static class AmmoTypeExtension
{
	public static string GetDescription( this AmmoType self )
	{
		if ( self == AmmoType.Pistol )
			return "Ammo for a pistol.";
		else if ( self == AmmoType.Bolt )
			return "Ammo for a crossbow.";
		else if ( self == AmmoType.SMG )
			return "Ammo for an SMG.";
		else if ( self == AmmoType.Rifle )
			return "Ammo for an assault rifle.";

		return string.Empty;
	}
}
