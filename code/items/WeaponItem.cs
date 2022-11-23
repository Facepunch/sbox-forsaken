using Sandbox;
using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

public class WeaponItem : ResourceItem<WeaponResource, WeaponItem>, IChildContainerItem
{
	public override Color Color => ItemColors.Weapon;

	public virtual int WorldModelMaterialGroup => Resource?.WorldModelMaterialGroup ?? 0;
	public virtual string WeaponName => Resource?.ClassName ?? string.Empty;
	public virtual int DefaultAmmo => Resource?.DefaultAmmo ?? 0;
	public virtual int ClipSize => Resource?.ClipSize ?? 0;
	public virtual int Damage => Resource?.Damage ?? 0;
	public virtual AmmoType AmmoType => Resource?.AmmoType ?? AmmoType.None;
	public virtual Curve RecoilCurve => Resource?.RecoilCurve ?? default;

	public InventoryContainer Attachments { get; private set; }
	public InventoryContainer ChildContainer => Attachments;
	public string ChildContainerName => "Attachments";

	public Weapon Weapon { get; set; }
	public int Ammo { get; set; }

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	public override void Write( BinaryWriter writer )
	{
		writer.WriteInventoryContainer( Attachments );

		if ( Weapon.IsValid() )
			writer.Write( Weapon.NetworkIdent );
		else
			writer.Write( 0 );

		writer.Write( Ammo );

		base.Write( writer );
	}

	public override void Read( BinaryReader reader )
	{
		Attachments = reader.ReadInventoryContainer();
		Weapon = (Entity.FindByIndex( reader.ReadInt32() ) as Weapon);
		Ammo = reader.ReadInt32();

		base.Read( reader );
	}

	public override void OnCreated()
	{
		if ( IsServer )
		{
			Attachments = new InventoryContainer();
			Attachments.SetSlotLimit( 4 );
			Attachments.SetParentItem( this );
			InventorySystem.Register( Attachments );
		}

		base.OnCreated();
	}

	public override void OnRemoved()
	{
		if ( IsServer && Weapon.IsValid() )
		{
			Weapon.Delete();
		}

		base.OnRemoved();
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "weapon" );

		base.BuildTags( tags );
	}
}
