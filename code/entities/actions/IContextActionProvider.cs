using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public interface IContextActionProvider : IValid
{
	public static List<ContextAction> GetAllActions( IContextActionProvider provider )
	{
		var allActions = new List<ContextAction>();

		var primary = provider.GetPrimaryAction();

		if ( primary.IsValid() )
			allActions.Add( primary );

		var secondary = provider.GetSecondaryActions();

		if ( secondary != null )
		{
			allActions.AddRange( secondary );
		}

		return allActions;
	}

	public float MaxInteractRange { get; }
	public Color GlowColor { get; }
	public float GlowWidth { get;}
	public List<ContextAction> GetSecondaryActions();
	public int NetworkIdent { get; }
	public ContextAction GetPrimaryAction();
	public Vector3 Position { get; }
	public string GetContextName();
}
