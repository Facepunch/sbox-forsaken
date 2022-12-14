using Sandbox;
using Sandbox.UI;
using System.Linq;

namespace Facepunch.Forsaken.UI;

[StyleSheet( "/ui/draggable/Draggable.scss" )]
public partial class Draggable : Panel
{
	private static Draggable Current { get; set; }

	public static void Start( IDraggable draggable, DraggableMode mode )
	{
		var active = Game.RootPanel.AddChild<Draggable>();
		active.SetMode( mode );
		active.SetDraggable( draggable );
		active.UpdatePosition();
	}

	public static void Stop( IDraggable draggable )
	{
		if ( Current?.ActiveDraggable == draggable )
		{
			if ( Current.IsActive )
			{
				Current.UpdateDroppable();

				if ( Current.ActiveDroppable?.CanDrop( draggable, Current.Mode ) ?? false )
				{
					Current.ActiveDroppable.OnDrop( Current.ActiveDraggable, Current.Mode );
				}
			}

			Current.Delete();
			Current = null;
		}
	}

	public DraggableMode Mode { get; private set; }
	public IDraggable ActiveDraggable { get; private set; }

	private TimeSince TimeSinceStarted { get; set; }
	private Vector3 StartPosition { get; set; }
	private IDroppable ActiveDroppable { get; set; }
	private bool IsActive { get; set; }

	public Draggable()
	{
		TimeSinceStarted = 0f;
		StartPosition = Mouse.Position;
		IsActive = false;

		Current?.Delete( true );
		Current = this;
	}

	public void UpdatePosition()
	{
		Style.Left = Length.Pixels( (Mouse.Position.x - 120f) * ScaleFromScreen );
		Style.Top = Length.Pixels( (Mouse.Position.y - 120f) * ScaleFromScreen );
	}

	public void SetMode( DraggableMode mode )
	{
		Mode = mode;
	}

	public void SetDraggable( IDraggable draggable )
	{
		ActiveDraggable = draggable;
		Style.SetBackgroundImage( draggable.GetIconTexture() );
		Style.Width = Length.Pixels( 80 * ScaleFromScreen );
		Style.Height = Length.Pixels( 80 * ScaleFromScreen );
	}

	public override void Tick()
	{
		SetClass( "active", IsActive );

		if ( !IsActive )
		{
			if ( TimeSinceStarted > 0.3f || Mouse.Position.Distance( StartPosition ) > 4f )
			{
				IsActive = true;
			}

			return;
		}

		UpdatePosition();
		UpdateDroppable();

		base.Tick();
	}

	public override void OnHotloaded()
	{
		Delete();
		Current = null;

		base.OnHotloaded();
	}

	public override void OnDeleted()
	{
		ActiveDroppable?.RemoveClass( "valid-drag" );
		ActiveDroppable?.RemoveClass( "invalid-drag" );

		base.OnDeleted();
	}

	public void UpdateDroppable()
	{
		var droppable = FindDroppable();

		if ( ActiveDroppable != null && droppable != ActiveDroppable )
		{
			ActiveDroppable.RemoveClass( "valid-drag" );
			ActiveDroppable.RemoveClass( "invalid-drag" );
		}

		ActiveDroppable = droppable;

		if ( ActiveDroppable != null )
		{
			if ( ActiveDroppable.CanDrop( ActiveDraggable, Mode ) )
				ActiveDroppable.AddClass( "valid-drag" );
			else
				ActiveDroppable.AddClass( "invalid-drag" );
		}
	}

	private IDroppable FindDroppable( Panel root = null )
	{
		root ??= Game.RootPanel;

		foreach ( var child in root.Children.Reverse() )
		{
			if ( !child.Box.Rect.IsInside( Mouse.Position ) )
				continue;

			if ( child.IsVisible )
			{
				var panel = FindDroppable( child );

				if ( panel != null )
					return panel;
			}
		}

		if ( root is IDroppable droppable )
		{
			return droppable;
		}

		return null;
	}
}
