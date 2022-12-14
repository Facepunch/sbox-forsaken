using Sandbox;

namespace Facepunch.Forsaken.UI;

public partial class LockScreen
{
    public static void OpenToLock( ForsakenPlayer player, ICodeLockable entity )
    {
        OpenForClient( To.Single(player), (Entity)entity, true );
    }

	public static void OpenToUnlock( ForsakenPlayer player, ICodeLockable entity )
	{
		OpenForClient( To.Single( player ), (Entity)entity, false );
	}

	[ClientRpc]
    public static void OpenForClient( Entity entity, bool isLockMode )
    {
        if ( Game.LocalPawn is not ForsakenPlayer ) return;
		if ( entity is not ICodeLockable lockable ) return;

        var lockScreen = Current;

		lockScreen.SetLockable( lockable, isLockMode );
		lockScreen.Open();

        Sound.FromScreen( "inventory.open" );
    }
}
