using Sandbox;
using Sandbox.UI;

namespace Facepunch.Forsaken.UI;

[StyleSheet( "/ui/StructureSelector.scss" )]
public partial class StructureSelector : RadialMenu
{
	public static StructureSelector Current { get; private set; }

	public override InputButton Button => InputButton.SecondaryAttack;

	public StructureSelector()
	{
		Current = this;
	}

	public override void Populate()
	{
		var descriptions = TypeLibrary.GetDescriptions<Structure>();

		foreach ( var type in descriptions )
		{
			if ( !type.IsAbstract )
			{
				var name = type.Name;
				var title = type.Title;
				var description = type.Description;
				AddItem( title, description, type.Icon, () => Select( name ) );
			}
		}

		base.Populate();
	}

	protected override bool ShouldOpen()
	{
		if ( !ForsakenPlayer.Me.IsValid() )
			return false;

		return (ForsakenPlayer.Me.GetActiveHotbarItem() is ToolboxItem);
	}

	private void Select( string typeName )
	{
		var type = TypeLibrary.GetDescription( typeName );
		ForsakenPlayer.Me.SetStructureType( type );
	}
}
