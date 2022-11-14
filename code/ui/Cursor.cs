using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Linq;

namespace Facepunch.Forsaken.UI;

public class CursorPrimaryAction : Panel
{
	private ContextAction Action { get; set; }
	private Image Icon { get; set; }
	private Label Title { get; set; }
	private Label Name { get; set; }

	public CursorPrimaryAction()
	{
		Icon = Add.Image( "", "icon" );
		Title = Add.Label( "", "title" );
		Name = Add.Label( "", "name" );

		BindClass( "visible", () => Action.IsValid() );
	}

	public bool Select()
	{
		var player = ForsakenPlayer.Me;

		if ( Action.IsValid() && Action.IsAvailable( player ) )
		{
			player.SetContextAction( Action );
			return true;
		}

		return false;
	}

	public void ClearAction()
	{
		Action = null;
	}

	public void SetAction( ContextAction action )
	{
		Assert.NotNull( action );

		if ( !string.IsNullOrEmpty( action.Icon ) )
		{
			Icon.Texture = Texture.Load( FileSystem.Mounted, action.Icon );
		}

		Title.Text = action.Provider.GetContextName();
		Name.Text = action.Name;

		Action = action;
	}
}

[StyleSheet( "/ui/Cursor.scss" )]
public class Cursor : Panel
{
	private IContextActionProvider ActionProvider { get; set; }
	private CursorPrimaryAction PrimaryAction { get; set; }

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

		var action = provider.GetPrimaryAction();

		if ( !action.IsValid() || !action.IsAvailable( ForsakenPlayer.Me ) )
		{
			action = provider.GetSecondaryActions().FirstOrDefault();
		}

		PrimaryAction.SetAction( action );
	}

	private void ClearActionProvider()
	{
		if ( !ActionProvider.IsValid() )
			return;

		PrimaryAction.ClearAction();

		ActionProvider = null;
	}

	[Event.BuildInput]
	private void BuildInput()
	{
		if ( Input.Released( InputButton.PrimaryAttack ) )
		{
			if ( ActionProvider.IsValid() && PrimaryAction.Select() )
			{
				return;
			}
		}
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

		PrimaryAction?.Delete();
		PrimaryAction = AddChild<CursorPrimaryAction>();

		base.OnParametersSet();
	}
}
