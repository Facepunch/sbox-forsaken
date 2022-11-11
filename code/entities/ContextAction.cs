using Sandbox;

namespace Facepunch.Forsaken;

public class ContextAction : BaseNetworkable
{
	public virtual string Name => "Action";
	public virtual string Description => "A simple contextual action.";

	public IContextActions Owner { get; private set; }

	public ContextAction()
	{

	}

	public ContextAction( IContextActions owner )
	{
		Owner = owner;
	}

	public void SetOwner( IContextActions owner )
	{
		Owner = owner;
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
