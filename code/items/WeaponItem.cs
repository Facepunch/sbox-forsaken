using Sandbox;
using System.Collections.Generic;
using System.IO;

namespace Facepunch.Forsaken;

public class WeaponItem : ResourceItem<WeaponResource, WeaponItem>, IContainerItem
{
	public override Color Color => ItemColors.Weapon;

	public virtual int WorldModelMaterialGroup => Resource?.WorldModelMaterialGroup ?? 0;
	public virtual string WeaponName => Resource?.ClassName ?? string.Empty;
	public virtual int DefaultAmmo => Resource?.DefaultAmmo ?? 0;
	public virtual int ClipSize => Resource?.ClipSize ?? 0;
	public virtual int Damage => Resource?.Damage ?? 0;
	public virtual AmmoType AmmoType => Resource?.AmmoType ?? AmmoType.None;
	public virtual Curve RecoilCurve => Resource?.RecoilCurve ?? default;

	public AttachmentContainer Attachments { get; private set; }
	public InventoryContainer Container => Attachments;
	public string ContainerName => "Attachments";

	public Weapon Weapon
	{
		get => InternalWeapon;
		set
		{
			if ( InternalWeapon != value )
			{
				InternalWeapon = value;
				OnWeaponChanged( value );
			}
		}
	}

	public int Ammo { get; set; }

	private Weapon InternalWeapon;

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
		Attachments = reader.ReadInventoryContainer() as AttachmentContainer;
		Weapon = (Entity.FindByIndex( reader.ReadInt32() ) as Weapon);
		Ammo = reader.ReadInt32();

		base.Read( reader );
	}

	public override void OnCreated()
	{
		if ( IsServer )
		{
			Attachments = new AttachmentContainer();
			Attachments.SetParent( this );
			InventorySystem.Register( Attachments );
		}

		base.OnCreated();
	}

	public override void OnRemoved()
	{
		if ( IsServer && Attachments.IsValid() )
		{
			InventorySystem.Remove( Attachments, true );
		}

		if ( IsServer && Weapon.IsValid() )
		{
			Weapon.Delete();
		}

		base.OnRemoved();
	}

	protected virtual void OnWeaponChanged( Weapon weapon )
	{
		foreach ( var attachment in Attachments.FindItems<AttachmentItem>() )
		{
			attachment.OnWeaponChanged( weapon );
		}
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "weapon" );

		base.BuildTags( tags );
	}
}
