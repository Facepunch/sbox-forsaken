﻿using Sandbox;
using Sandbox.UI;

namespace Facepunch.Forsaken;

[StyleSheet( "/ui/StructureSelector.scss" )]
public partial class StructureSelector : RadialMenu
{
	public static StructureSelector Current { get; private set; }

	public override InputButton Button => InputButton.View;

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
		return Local.Pawn is Player;
	}

	private void Select( string typeName )
	{
		if ( Local.Pawn is Player player )
		{
			var type = TypeLibrary.GetDescription( typeName );
			player.SetStructureType( type );
		}
	}
}