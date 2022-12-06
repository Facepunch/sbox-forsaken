using Sandbox;
using System;
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public interface IContextActionProvider : IValid
{
	public static IEnumerable<ContextAction> GetAllActions( ForsakenPlayer player, IContextActionProvider provider )
	{
		var primary = provider.GetPrimaryAction( player );

		if ( primary.IsValid() )
		{
			yield return primary;
		}

		var secondary = provider.GetSecondaryActions( player );

		foreach ( var action in secondary )
		{
			yield return action;
		}
	}

	public float InteractionRange { get; }
	public Color GlowColor { get; }
	public float GlowWidth { get;}
	public IEnumerable<ContextAction> GetSecondaryActions( ForsakenPlayer player );
	public int NetworkIdent { get; }
	public ContextAction GetPrimaryAction( ForsakenPlayer player );
	public Vector3 Position { get; }
	public string GetContextName();
	public void OnContextAction( ForsakenPlayer player, ContextAction action );
}
