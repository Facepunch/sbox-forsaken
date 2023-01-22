using Sandbox;
using System.IO;

namespace Facepunch.Forsaken;

public abstract partial class UpgradableStructure : Structure
{
	protected virtual int StoneUpgradeCost => 200;
	protected virtual int MetalUpgradeCost => 100;

	private ContextAction StoneUpgradeAction { get; set; }
	private ContextAction MetalUpgradeAction { get; set; }

	[Net] public StructureMaterial Material { get; private set; }

	public UpgradableStructure() : base()
	{
		StoneUpgradeAction = new( "upgrade.stone", $"Upgrade", "textures/items/stone.png" );
		StoneUpgradeAction.SetCondition( p =>
		{
			return new ContextAction.Availability
			{
				IsAvailable = p.HasItems<StoneItem>( StoneUpgradeCost ),
				Message = $"{StoneUpgradeCost} x Stone"
			};
		} );

		MetalUpgradeAction = new( "upgrade.metal", $"Upgrade", "textures/items/metal_fragments.png" );
		MetalUpgradeAction.SetCondition( p =>
		{
			return new ContextAction.Availability
			{
				IsAvailable = p.HasItems<MetalFragments>( MetalUpgradeCost ),
				Message = $"{MetalUpgradeCost} x Metal Fragments"
			};
		} );
	}

	public override void SerializeState( BinaryWriter writer )
	{
		base.SerializeState( writer );

		writer.Write( (byte)Material );
	}

	public override void DeserializeState( BinaryReader reader )
	{
		base.DeserializeState( reader );

		Material = (StructureMaterial)reader.ReadByte();
	}

	public override void BeforeStateLoaded()
	{
		UpdateMaterial();

		base.BeforeStateLoaded();
	}

	public override ContextAction GetPrimaryAction( ForsakenPlayer player )
	{
		var hotbarItem = player.GetActiveHotbarItem();

		if ( hotbarItem is HammerItem )
		{
			if ( Material == StructureMaterial.Wood )
				return StoneUpgradeAction;
			else if ( Material == StructureMaterial.Stone )
				return MetalUpgradeAction;
		}

		return default;
	}

	public override void OnContextAction( ForsakenPlayer player, ContextAction action )
	{
		if ( Game.IsClient ) return;

		if ( action == StoneUpgradeAction && Material == StructureMaterial.Wood )
		{
			if ( player.HasItems<StoneItem>( StoneUpgradeCost ) )
			{
				Sound.FromWorld( To.Everyone, PlaceSoundName, Position );
				player.TakeItems<StoneItem>( StoneUpgradeCost );
				Material = StructureMaterial.Stone;
				UpdateMaterial();
			}
		}
		else if ( action == MetalUpgradeAction && Material == StructureMaterial.Stone )
		{
			Sound.FromWorld( To.Everyone, PlaceSoundName, Position );
			player.TakeItems<MetalFragments>( MetalUpgradeCost );
			Material = StructureMaterial.Metal;
			UpdateMaterial();
		}
	}

	protected virtual void UpdateMaterial()
	{
		if ( Material == StructureMaterial.Stone )
			SetMaterialGroup( "stone" );
		else if ( Material == StructureMaterial.Metal )
			SetMaterialGroup( "metal" );
	}
}
