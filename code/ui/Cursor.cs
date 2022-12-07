using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
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
	private bool DisableSecondaryActions { get; set; }
	private Panel ActionContainer { get; set; }
	private bool IsSecondaryOpen { get; set; }
	private Vector2 ActionCursorPosition { get; set; }
	private TimeSince LastActionTime { get; set; }
	private int ActionHash { get; set; }
	private Panel PlusMoreIcon { get; set; }
	private Panel ActionCursor { get; set; }
	private Label Title { get; set; }

	public Cursor()
	{
		LastActionTime = 0f;
		PrimaryAction = AddChild<CursorAction>( "primary-action" );
		PlusMoreIcon = Add.Panel( "plus-more" );
		ActionContainer = Add.Panel( "actions" );
		Title = Add.Label( "", "title" );
		ActionCursor = Add.Panel( "action-cursor" );
	}

	public override void Tick()
	{
		var player = ForsakenPlayer.Me;

		if ( !player.IsValid() ) return;

		Style.Left = Length.Fraction( player.Cursor.x );
		Style.Top = Length.Fraction( player.Cursor.y );

		var provider = player.HoveredEntity as IContextActionProvider;

		if ( player.HasTimedAction )
		{
			LastActionTime = 0f;
		}

		SetClass( "recent-action", LastActionTime < 0.5f );

		if ( LastActionTime > 0.5f && provider.IsValid() && player.Position.Distance( provider.Position ) <= provider.InteractionRange )
			SetActionProvider( provider );
		else
			ClearActionProvider();
	}

	private int GetActionHash( ContextAction primary, IEnumerable<ContextAction> secondaries )
	{
		var hash = 0;

		if ( primary.IsValid() )
		{
			hash = HashCode.Combine( hash, primary, primary.IsAvailable( ForsakenPlayer.Me ) );
		}

		foreach ( var action in secondaries )
		{
			hash = HashCode.Combine( hash, action, action.IsAvailable( ForsakenPlayer.Me ) );
		}

		return hash;
	}

	private void SetActionProvider( IContextActionProvider provider )
	{
		var primary = provider.GetPrimaryAction( ForsakenPlayer.Me );
		var secondaries = provider.GetSecondaryActions( ForsakenPlayer.Me );
		var hash = GetActionHash( primary, secondaries );

		if ( ActionProvider == provider && ActionHash == hash )
		{
			return;
		}

		ActionProvider = provider;
		ActionHash = hash;

		if ( !primary.IsValid() || !primary.IsAvailable( ForsakenPlayer.Me ) )
		{
			primary = secondaries.FirstOrDefault( s => s.IsAvailable( ForsakenPlayer.Me ) );

			if ( !primary.IsValid() )
			{
				ClearActionProvider();
				return;
			}	
		}

		ActionContainer.DeleteChildren( true );

		foreach ( var secondary in secondaries )
		{
			if ( secondary == primary )
				continue;

			var action = new CursorAction();
			action.SetAction( secondary );
			ActionContainer.AddChild( action );
		}

		PrimaryAction.SetAction( primary );

		Title.Text = provider.GetContextName();

		SetClass( "was-deleted", false );
		SetClass( "has-secondary", ActionContainer.ChildrenCount > 0 );
		SetClass( "has-actions", true );
	}

	private void ClearActionProvider()
	{
		if ( ActionProvider == null )
			return;

		ActionContainer.DeleteChildren( true );
		PrimaryAction.ClearAction();

		SetClass( "was-deleted", !ActionProvider.IsValid() );
		SetClass( "has-secondary", false );
		SetClass( "has-actions", false );

		ActionProvider = null;
	}

	[Event.Client.BuildInput]
	private void BuildInput()
	{
		var hasSecondaries = ActionContainer.ChildrenCount > 0;
		var secondaryHoldDelay = 0.25f;

		if ( !ActionProvider.IsValid() || IsHidden() || LastActionTime < 0.5f )
		{
			DisableSecondaryActions = true;
			IsSecondaryOpen = false;
			return;
		}

		if ( Input.Pressed( InputButton.PrimaryAttack ) )
		{
			DisableSecondaryActions = false;
			TimeSincePressed = 0f;
			IsSecondaryOpen = false;
		}

		if ( !DisableSecondaryActions )
		{
			if ( Input.Down( InputButton.PrimaryAttack ) && hasSecondaries )
			{
				if ( TimeSincePressed > secondaryHoldDelay && !IsSecondaryOpen )
				{
					ActionCursorPosition = Vector2.Zero;
					IsSecondaryOpen = true;
				}
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
				LastActionTime = 0f;
				return;
			}
		}
	}

	private void UpdateActionCursor()
	{
		var mouseDelta = Input.MouseDelta;

		ActionCursorPosition += (mouseDelta * 10f * Time.Delta);
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
				if ( closestItem.Select() )
				{
					LastActionTime = 0f;
				}
			}
		}

		if ( !Input.Down( InputButton.PrimaryAttack ) )
		{
			DisableSecondaryActions = true;
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

		if ( player.HasTimedAction )
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
