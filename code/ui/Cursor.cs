using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Linq;

namespace Facepunch.Forsaken.UI;

public class CursorAction : Panel
{
	public ContextAction Action { get; private set; }

	private Image Icon { get; set; }
	private Label Name { get; set; }

	public CursorAction()
	{
		Icon = Add.Image( "", "icon" );
		Name = Add.Label( "", "name" );

		BindClass( "visible", () => Action.IsValid() );
		BindClass( "unavailable", () => !Action.IsValid() || !Action.IsAvailable( ForsakenPlayer.Me ) );
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
		Action = default;
	}

	public void SetAction( ContextAction action )
	{
		if ( !string.IsNullOrEmpty( action.Icon ) )
		{
			Icon.Texture = Texture.Load( FileSystem.Mounted, action.Icon );
		}

		Name.Text = action.Name;

		Action = action;
	}
}

[StyleSheet( "/ui/Cursor.scss" )]
public class Cursor : Panel
{
	private IContextActionProvider ActionProvider { get; set; }
	private CursorAction PrimaryAction { get; set; }
	private TimeSince TimeSincePressed { get; set; }
	private Panel ActionContainer { get; set; }
	private bool IsSecondaryOpen { get; set; }
	private Vector2 ActionCursorPosition { get; set; }
	private Panel PlusMoreIcon { get; set; }
	private Panel ActionCursor { get; set; }
	private Label Title { get; set; }

	public Cursor()
	{
		PrimaryAction = AddChild<CursorAction>( "primary-action" );
		PlusMoreIcon = Add.Panel( "plus-more" );
		ActionContainer = Add.Panel( "actions" );
		Title = Add.Label( "", "title" );
		ActionCursor = Add.Panel( "action-cursor" );
	}

	public override void Tick()
	{
		var player = ForsakenPlayer.Me;

		if ( player.IsValid() )
		{
			Style.Left = Length.Fraction( player.Cursor.x );
			Style.Top = Length.Fraction( player.Cursor.y );

			var provider = player.HoveredEntity as IContextActionProvider;

			if ( provider.IsValid() && player.Position.Distance( provider.Position ) <= provider.InteractionRange )
				SetActionProvider( provider );
			else
				ClearActionProvider();
		}

		base.Tick();
	}

	private void SetActionProvider( IContextActionProvider provider )
	{
		if ( ActionProvider == provider )
		{
			if ( PrimaryAction.Action == provider.GetPrimaryAction() )
				return;
		}

		ActionProvider = provider;

		var primary = provider.GetPrimaryAction();
		var secondaries = provider.GetSecondaryActions();

		if ( !primary.IsValid() || !primary.IsAvailable( ForsakenPlayer.Me ) )
		{
			primary = secondaries.FirstOrDefault();
		}

		ActionContainer.DeleteChildren( true );

		foreach ( var secondary in secondaries )
		{
			var action = new CursorAction();
			action.SetAction( secondary );
			ActionContainer.AddChild( action );
		}

		PrimaryAction.SetAction( primary );

		Title.Text = provider.GetContextName();

		SetClass( "has-secondary", secondaries.Any() );
		SetClass( "has-actions", true );
	}

	private void ClearActionProvider()
	{
		if ( ActionProvider == null )
			return;

		ActionContainer.DeleteChildren( true );
		PrimaryAction.ClearAction();

		ActionProvider = null;

		SetClass( "has-secondary", false );
		SetClass( "has-actions", false );
	}

	[Event.BuildInput]
	private void BuildInput()
	{
		var hasSecondaries = ActionContainer.ChildrenCount > 0;
		var secondaryHoldDelay = 0.25f;

		if ( !ActionProvider.IsValid() )
		{
			IsSecondaryOpen = false;
			return;
		}

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
		{
			TimeSincePressed = 0f;
			IsSecondaryOpen = false;
		}

		if ( Input.Down( InputButton.PrimaryAttack ) && hasSecondaries )
		{
			if ( TimeSincePressed > secondaryHoldDelay && !IsSecondaryOpen )
			{
				ActionCursorPosition = Vector2.Zero;
				IsSecondaryOpen = true;
			}
		}

		if ( IsSecondaryOpen )
		{
			UpdateActionCursor();
			return;
		}

		if ( Input.Released( InputButton.PrimaryAttack ) && ( !hasSecondaries || TimeSincePressed < secondaryHoldDelay ) )
		{
			if ( PrimaryAction.Select() )
			{
				return;
			}
		}
	}

	private void UpdateActionCursor()
	{
		var mouseDelta = Input.MouseDelta;
		var sensitivity = 0.06f;

		ActionCursorPosition += (mouseDelta * sensitivity);
		ActionCursorPosition = ActionCursorPosition.Clamp( Vector2.One * -500f, Vector2.One * 500f );

		CursorAction closestItem = null;
		var closestDistance = 0f;
		var globalPosition = Box.Rect.Center + ActionCursorPosition;

		var children = ActionContainer.ChildrenOfType<CursorAction>();

		foreach ( var child in children )
		{
			var distance = child.Box.Rect.Center.Distance( globalPosition );

			if ( distance <= 32f && (closestItem == null || distance < closestDistance ) )
			{
				closestDistance = distance;
				closestItem = child;
			}

			child.SetClass( "is-hovered", false );
		}

		ActionCursor.Style.Left = Length.Pixels( ActionCursorPosition.x * ScaleFromScreen );
		ActionCursor.Style.Top = Length.Pixels( ActionCursorPosition.y * ScaleFromScreen );

		if ( closestItem != null )
		{
			closestItem.SetClass( "is-hovered", true );

			if ( Input.Released( InputButton.PrimaryAttack ) )
			{
				closestItem.Select();
			}
		}

		if ( !Input.Down( InputButton.PrimaryAttack ) )
		{
			IsSecondaryOpen = false;
		}

		Input.StopProcessing = true;
		Input.AnalogMove = Vector2.Zero;
		Input.AnalogLook = Angles.Zero;
	}

	private bool IsHidden()
	{
		var player = ForsakenPlayer.Me;

		if ( !player.IsValid() || player.LifeState == LifeState.Dead )
			return true;

		if ( ToolboxMenu.Current?.IsOpen ?? false )
			return true;

		if ( ReloadMenu.Current?.IsOpen ?? false )
			return true;

		if ( Dialog.IsActive() )
			return true;

		return false;
	}

	protected override void OnParametersSet()
	{
		BindClass( "secondary-open", () => IsSecondaryOpen );
		BindClass( "hidden", IsHidden );

		base.OnParametersSet();
	}
}
