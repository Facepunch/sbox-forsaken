using Sandbox;

namespace Facepunch.Forsaken;

public partial class ItemEntity : ModelEntity, IContextActions
{
	[Net] public NetInventoryItem Item { get; private set; }

	public TimeUntil TimeUntilCanPickup { get; set; }

	public Color GlowColor => Item.Value?.Color ?? Color.White;
	public float GlowWidth => 0.4f;

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
			var item = Item.Value;

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

