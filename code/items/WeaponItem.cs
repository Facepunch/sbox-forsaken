using Sandbox;
using System.IO;

namespace Facepunch.Forsaken;

public class WeaponItem : InventoryItem
{
	public override Color Color => ItemColors.Weapon;

	public virtual int WorldModelMaterialGroup => 0;
	public virtual int ViewModelMaterialGroup => 0;
	public virtual string WorldModelPath => null;
	public virtual string ViewModelPath => null;
	public virtual string WeaponName => string.Empty;
	public virtual string Group => string.Empty;
	public virtual int Tier => 0;

	public Weapon Weapon { get; set; }
	public int Ammo { get; set; }

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	public override void Write( BinaryWriter writer )
	{
		if ( Weapon.IsValid() )
			writer.Write( Weapon.NetworkIdent );
		else
			writer.Write( 0 );

		writer.Write( Ammo );

		base.Write( writer );
	}

	public override void Read( BinaryReader reader )
	{
		Weapon = (Entity.FindByIndex( reader.ReadInt32() ) as Weapon);
		Ammo = reader.ReadInt32();

		base.Read( reader );
	}

	public override void OnRemoved()
	{
		if ( IsServer && Weapon.IsValid() )
		{
			Weapon.Delete();
		}

		base.OnRemoved();
	}
}
