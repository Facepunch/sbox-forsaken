namespace Facepunch.Forsaken;

public class OpenAction : ContextAction
{
	public override string UniqueId => "open";
	public override string Name => "Open";
	public override string Icon => "textures/ui/armor_slot_head.png";

	public OpenAction()
	{

	}

	public OpenAction( IContextActionProvider owner )
	{
		Provider = owner;
	}

	public override void Select( ForsakenPlayer player )
	{
		Log.Info( "You opened it!" );
	}
}
