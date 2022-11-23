﻿using Sandbox;
using System.IO;

namespace Facepunch.Forsaken;

public static class BinaryWriterExtension
{
	public static void WriteInventoryItem( this BinaryWriter writer, InventoryItem item )
	{
		if ( item != null )
		{
			writer.Write( item.UniqueId );
			writer.Write( item.StackSize );
			writer.Write( item.ItemId );
			writer.Write( item.SlotId );

			item.Write( writer );
		}
		else
		{
			writer.Write( string.Empty );
		}
	}

	public static void WriteInventoryContainer( this BinaryWriter writer, InventoryContainer container )
	{
		var typeDesc = TypeLibrary.GetDescription( container.GetType() );

		writer.Write( typeDesc.Identity );
		writer.Write( container.ParentItemId );
		writer.Write( container.InventoryId );
		writer.Write( container.SlotLimit );

		if ( container.Entity.IsValid() )
			writer.Write( container.Entity );
		else
			writer.Write( -1 );

		for ( var i = 0; i < container.SlotLimit; i++ )
		{
			var instance = container.ItemList[i];

			if ( instance != null )
			{
				writer.Write( true );
				writer.WriteInventoryItem( instance );
			}
			else
			{
				writer.Write( false );
			}
		}

		container.Serialize( writer );
	}
}
