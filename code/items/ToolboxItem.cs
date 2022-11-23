using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class ToolboxItem : InventoryItem
{
	public override Color Color => ItemColors.Tool;
	public override string Description => "A useful set of tools for construction.";
	public override string UniqueId => "toolbox";
	public override string Name => "Toolbox";
	public override string Icon => "textures/items/toolbox.png";

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
