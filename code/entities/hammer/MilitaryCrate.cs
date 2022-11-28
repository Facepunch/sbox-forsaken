using Sandbox;
using SandboxEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Facepunch.Forsaken;

[HammerEntity]
[Title( "Military Crate" )]
[EditorModel( "models/military_crate/military_crate.vmdl" )]
public partial class MilitaryCrate : LootSpawner
{
	public override string ModelPath => "models/military_crate/military_crate.vmdl";
	public override string Title => "Military Crate";
	public override float RestockTime => 30f;
	public override int SlotLimit => 6;

	protected override void Restock()
	{
		var possibleItems = InventorySystem.GetDefinitions()
			.OfType<ILootTableItem>()
			.Where( i => i.IsLootable )
			.Where( i => i.SpawnChance > 0f );

		if ( !possibleItems.Any() ) return;

		var itemsToSpawn = Rand.Int( 1, SlotLimit );

		for ( var i = 0; i < itemsToSpawn; i++ )
		{
			ILootTableItem item = null;

			while ( item == null )
			{
				var items = possibleItems.Where( i => Rand.Float() < i.SpawnChance ).ToList();
				if ( items.Count == 0 ) continue;

				item = Rand.FromList( items );

				var instance = InventorySystem.CreateItem( item.UniqueId );
				instance.StackSize = (ushort)item.AmountToSpawn;

				Inventory.Give( instance );
			}
		}
	}
}
