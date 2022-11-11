using Sandbox;
using Sandbox.UI;

namespace Facepunch.Forsaken.UI;

[StyleSheet( "/ui/Cursor.scss" )]
public class Cursor : Panel
{
	private IContextActionProvider ActionProvider { get; set; }

	public override void Tick()
	{
		var player = ForsakenPlayer.Me;

		if ( player.IsValid() )
		{
			Style.Left = Length.Fraction( player.Cursor.x );
			Style.Top = Length.Fraction( player.Cursor.y );

			if ( player.HoveredEntity is IContextActionProvider provider )
				SetActionProvider( provider );
			else
				ClearActionProvider();
		}

		base.Tick();
	}

	private void SetActionProvider( IContextActionProvider provider )
	{
		if ( ActionProvider == provider )
			return;

		ActionProvider = provider;

		Log.Info( "Our Provider Is: " + provider );
	}

	private void ClearActionProvider()
	{
		if ( !ActionProvider.IsValid() )
			return;

		Log.Info( "Cleared Provider" );

		ActionProvider = null;
	}

	private bool IsHidden()
	{
		var player = ForsakenPlayer.Me;

		if ( !player.IsValid() || player.LifeState == LifeState.Dead )
			return true;

		if ( StructureSelector.Current?.IsOpen ?? false )
			return true;

		if ( IDialog.IsActive() )
			return true;

		return false;
	}

	protected override void OnParametersSet()
	{
		BindClass( "hidden", IsHidden );

		base.OnParametersSet();
	}
}
