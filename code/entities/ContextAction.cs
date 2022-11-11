using Sandbox;

namespace Facepunch.Forsaken;

public class ContextAction : BaseNetworkable
{
	public virtual string Name => "Action";

	public IContextActionProvider Provider { get; private set; }

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
