using Sandbox;

namespace Facepunch.Forsaken;

public class ContextAction : BaseNetworkable, IValid
{
	public virtual string Name => "Action";
	public virtual string Icon => "textures/ui/armor_slot_head.png";

	public IContextActionProvider Provider { get; private set; }

	public bool IsValid => Provider.IsValid();

	public ContextAction()
	{

	}

	public ContextAction( IContextActionProvider owner )
	{
		Provider = owner;
	}

	public void SetOwner( IContextActionProvider owner )
	{
		Provider = owner;
	}

	public virtual void Select( ForsakenPlayer player )
	{
		Log.Info( "You selected it!" );
	}

	public virtual bool IsAvailable( ForsakenPlayer player )
	{
		return true;
	}
}
