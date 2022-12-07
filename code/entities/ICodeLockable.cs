using Sandbox;
using System;
using System.Linq;

namespace Facepunch.Forsaken;

public interface ICodeLockable : IValid
{
	[ConCmd.Server( "fsk.code.apply" )]
	public static void ApplyLock( int entityId, string code )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player )
			return;

		if ( !code.All( char.IsDigit ) || code.Length != 4 )
			return;

		var lockable = Entity.FindByIndex( entityId ) as ICodeLockable;

		if ( lockable.IsValid() && !lockable.IsLocked && lockable.IsAuthorized( player ) )
		{
			lockable.ApplyLock( player, code );
			player.PlaySound( "authorize.success" );
		}
	}

	[ConCmd.Server( "fsk.code.authorize" )]
	public static void Authorize( int entityId, string code )
	{
		if ( ConsoleSystem.Caller.Pawn is not ForsakenPlayer player )
			return;

		var lockable = Entity.FindByIndex( entityId ) as ICodeLockable;

		if ( lockable.IsValid() && lockable.IsLocked && lockable.Code == code )
		{
			if ( !lockable.IsAuthorized( player ) )
			{
				lockable.Authorize( player );
				player.PlaySound( "authorize.success" );
			}
		}
		else
		{
			player.PlaySound( "authorize.fail" );
		}
	}

	public bool IsAuthorized( ForsakenPlayer player );
	public bool ApplyLock( ForsakenPlayer player, string code );
	public void Authorize( ForsakenPlayer player );
	public int NetworkIdent { get; }
	public bool IsLocked { get; }
	public string Code { get; }
}
