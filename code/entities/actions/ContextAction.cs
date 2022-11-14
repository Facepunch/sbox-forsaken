using Sandbox;

namespace Facepunch.Forsaken;

public abstract class ContextAction : BaseNetworkable, IValid
{
	public abstract string Name { get; }
	public virtual string Icon => "";

	public bool IsServer => Host.IsServer;
	public bool IsClient => Host.IsClient;

	public IContextActionProvider Provider { get; protected set; }

	public bool IsValid => Provider.IsValid();

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
