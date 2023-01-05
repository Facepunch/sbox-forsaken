using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class StoneItem : InventoryItem
{
	public override Color Color => ItemColors.Material;
	public override string Name => "Stone";
	public override string UniqueId => "stone";
	public override string Description => "A bunch of stones. Usually obtained by smashing rocks until they break.";
	public override ushort MaxStackSize => 100;
	public override string Icon => "textures/items/stone.png";

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "material" );

		base.BuildTags( tags );
	}
}
