using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class StockpileItem : DeployableItem
{
	public override Type Deployable => typeof( Stockpile );
	public override string[] ValidTags => new string[] { "foundation" };
	public override string Model => "models/stockpile/stockpile.vmdl";
	public override string Description => "The heart of your home. You can store your materials in it, and it will prevent unauthorized players from building near your home.";
	public override string UniqueId => "stockpile";
	public override string Icon => "textures/items/stockpile.png";
	public override string Name => "Stockpile";

	public override bool CanPlaceOn( Entity entity )
	{
		if ( entity is Foundation foundation )
		{
			if ( foundation.Stockpile.IsValid() )
				return false;

			return true;
		}

		return false;
	}

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
