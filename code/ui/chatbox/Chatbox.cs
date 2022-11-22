using Sandbox;
using Sandbox.UI;

namespace Facepunch.Forsaken.UI;

public partial class Chatbox : Panel
{
	private static Chatbox Instance;

	public Chatbox()
	{
		Instance = this;
	}
	
	[ConCmd.Server]
	public static void SendChat( string message )
	{
		if ( !ConsoleSystem.Caller.IsValid() ) return;

		if ( ConsoleSystem.Caller.Pawn is ForsakenPlayer player )
		{
			var recipients = player.GetChatRecipients();
			AddChat( To.Multiple( recipients ), ConsoleSystem.Caller.Name, message );
		}
	}

	[ConCmd.Server( "fsk.chat.system" )]
	public static void AddSystemMsgCmd( string msg )
	{
		AddSystem( msg );
	}

	[ClientRpc]
	public static void AddSystem( string message )
	{
		Instance.AddMessage( message, "system" );
	}

	[ConCmd.Client( "fsk.say", CanBeCalledFromServer = true )]
	public static void AddChat( string name, string message )
	{
		Instance.AddNamedMessage( name, message );
	}
}
