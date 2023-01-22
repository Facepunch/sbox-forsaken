using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class HammerItem : InventoryItem
{
	public override Color Color => ItemColors.Tool;

	public override string Description => "A simple tool for upgrading or repairing.";
	public override string UniqueId => "hammer";
	public override string Name => "Hammer";
	public override string Icon => "textures/items/hammer.png";

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}

	protected override void BuildTags( HashSet<string> tags )
	{
		tags.Add( "tool" );

		base.BuildTags( tags );
	}
}
