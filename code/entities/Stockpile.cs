using Sandbox;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Facepunch.Forsaken;

public partial class Stockpile : Deployable, IContextActionProvider, IPersistence, ICodeLockable
{
	public float InteractionRange => 150f;
	public Color GlowColor => Color.Green;
	public float GlowWidth => 0.4f;

	[Net] private NetInventoryContainer InternalInventory { get; set; }
	public InventoryContainer Inventory => InternalInventory.Value;

	private ContextAction OpenAction { get; set; }
	private ContextAction LockAction { get; set; }
	private ContextAction AuthorizeAction { get; set; }

	[Net] private IList<long> Authorized { get; set; } = new List<long>();
	[Net] public bool IsLocked { get; private set; }

	public string Code { get; private set; }

	public Stockpile()
	{
		OpenAction = new( "open", "Open", "textures/ui/actions/open.png" );

		LockAction = new( "lock", "Lock", "textures/items/code_lock.png" );
		LockAction.SetCondition( CanBeLockedBy );

		AuthorizeAction = new( "authorize", "Authorize", "textures/ui/actions/authorize.png" );
	}

	public bool ShouldSaveState()
	{
		return true;
	}

	public void BeforeStateLoaded()
	{

	}

	public void AfterStateLoaded()
	{
		var foundation = FindInSphere( Position, 64f )
			.OfType<Foundation>()
			.FirstOrDefault();

		if ( foundation.IsValid() )
		{
			foundation.PropagateStockpile( this );
		}
	}

	public void SerializeState( BinaryWriter writer )
	{
		writer.Write( Transform );
		writer.Write( Inventory );
		writer.Write( IsLocked );
		writer.Write( string.IsNullOrEmpty( Code ) ? "" : Code );
		writer.Write( Authorized.Count );

		foreach ( var id in Authorized )
		{
			writer.Write( id );
		}
	}

	public void DeserializeState( BinaryReader reader )
	{
		Transform = reader.ReadTransform();

		var container = reader.ReadInventoryContainer();
		InternalInventory = new( container );

		IsLocked = reader.ReadBoolean();
		Code = reader.ReadString();

		var count = reader.ReadInt32();

		for ( var i = 0; i < count; i++ )
		{
			var id = reader.ReadInt64();
			Authorized.Add( id );
		}
	}

	public string GetContextName()
	{
		return "Stockpile";
	}

	public void Open( ForsakenPlayer player )
	{
		UI.Storage.Open( player, GetContextName(), this, Inventory );
	}

	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player )
	{
		if ( !IsLocked )
		{
			yield return LockAction;
		}
	}

	public ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		if ( IsLocked && !IsAuthorized( player ) )
			return AuthorizeAction;

		return OpenAction;
	}

	public bool ApplyLock( ForsakenPlayer player, string code )
	{
		if ( !player.HasItems<CodeLockItem>( 1 ) )
			return false;

		IsLocked = true;
		Code = code;

		player.TakeItems<CodeLockItem>( 1 );

		return true;
	}

	public void Authorize( ForsakenPlayer player )
	{
		if ( IsAuthorized( player ) ) return;
		Authorized.Add( player.SteamId );
	}

	public bool CanBeLockedBy( ForsakenPlayer player )
	{
		return IsAuthorized( player ) && player.HasItems<CodeLockItem>( 1 );
	}

	public void Deauthorize( ForsakenPlayer player )
	{
		Authorized.Remove( player.SteamId );
	}

	public bool IsAuthorized( ForsakenPlayer player )
	{
		return Authorized.Contains( player.SteamId );
	}

	public bool IsAuthorized()
	{
		Game.AssertClient();
		return Authorized.Contains( Game.LocalClient.SteamId );
	}

	public virtual void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( Game.IsClient ) return;

		if ( action == OpenAction )
		{
			Open( player );
		}
		else if ( action == LockAction && IsAuthorized( player ) )
		{
			UI.LockScreen.OpenToLock( player, this );
		}
		else if ( action == AuthorizeAction )
		{
			UI.LockScreen.OpenToUnlock( player, this );
		}
	}

	public override void OnPlacedByPlayer( ForsakenPlayer player, TraceResult trace )
	{
		Authorize( player );

		if ( trace.Entity is Foundation foundation )
		{
			Log.Info( "Placed Stockpile on Foundation" );
			foundation.PropagateStockpile( this );
		}

		base.OnPlacedByPlayer( player, trace );
	}

	public override void Spawn()
	{
		SetModel( "models/stockpile/stockpile.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		var inventory = new InventoryContainer();
		inventory.SetEntity( this );
		inventory.Whitelist.Add( "material" );
		inventory.SetSlotLimit( 24 );
		InventorySystem.Register( inventory );

		InternalInventory = new NetInventoryContainer( inventory );

		Tags.Add( "hover", "solid" );

		base.Spawn();
	}
}
