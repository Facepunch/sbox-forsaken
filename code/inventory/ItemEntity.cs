using Sandbox;

namespace Facepunch.Forsaken;

public partial class ItemEntity : ModelEntity
{
	[Net] public NetInventoryItem Item { get; private set; }

	public TimeUntil TimeUntilCanPickup { get; set; }

	public void SetItem( InventoryItem item )
	{
		if ( !string.IsNullOrEmpty( item.WorldModel ) )
		{
			SetModel( item.WorldModel );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		}

		Item = new NetInventoryItem( item );
		item.SetWorldEntity( this );

		Tags.Add( "item" );
	}

	public InventoryItem Take()
	{
		if ( IsValid )
		{
			var item = Item.Instance;

			item.ClearWorldEntity();
			Item = null;
			Delete();

			return item;
		}

		return null;
	}

	public virtual void Reset()
	{
		Delete();
	}

	public override void Spawn()
	{
		TimeUntilCanPickup = 1f;
		base.Spawn();
	}
}

