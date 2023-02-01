using Sandbox;
using System;

namespace Facepunch.Forsaken.UI;

public partial class Death
{
	public static void Show( ForsakenPlayer player, DamageInfo info, int timeAliveFor )
	{
		var attackerName = string.Empty;
		var weaponIcon = string.Empty;

		if ( info.Attacker.IsValid() && info.Attacker is ForsakenPlayer attacker )
		{
			attackerName = attacker.DisplayName;
		}

		if ( info.Weapon.IsValid() && info.Weapon is Weapon weapon )
		{
			weaponIcon = weapon.WeaponItem?.Icon ?? string.Empty;
		}
		else if ( info.HasTag( "hunger" ) )
		{
			attackerName = "Hunger";
			weaponIcon = "textures/ui/hunger.png";
		}
		else if ( info.HasTag( "thirst" ) )
		{
			attackerName = "Dehydration";
			weaponIcon = "textures/ui/thirst.png";
		}
		else
		{
			attackerName = "Unknown";
			weaponIcon = "textures/ui/skull.png";
		}

		OpenForClient( To.Single( player ), attackerName, weaponIcon, timeAliveFor );
	}

	[ClientRpc]
	public static void OpenForClient( string attackerName, string weaponIcon, int timeAliveFor )
	{
		var death = Current;

		death.AttackerName = attackerName;
		death.WeaponIcon = weaponIcon;
		death.TimeAliveFor = timeAliveFor;
	}
}
