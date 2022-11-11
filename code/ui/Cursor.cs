using Sandbox;
using Sandbox.UI;

namespace Facepunch.Forsaken.UI;

[StyleSheet( "/ui/Cursor.scss" )]
public class Cursor : Panel
{
	public override void Tick()
	{
		var player = ForsakenPlayer.Me;

		if ( player.IsValid() )
		{
			Style.Left = Length.Fraction( player.Cursor.x );
			Style.Top = Length.Fraction( player.Cursor.y );
		}

		base.Tick();
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
