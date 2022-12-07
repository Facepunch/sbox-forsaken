using Sandbox;

namespace Facepunch.Forsaken.UI;

public static class Dialog
{
	public static IDialog Active { get; private set; }

	public static void Activate( IDialog dialog )
	{
		if ( Active != dialog )
		{
			Active = dialog;
		}
	}

	public static void Deactivate( IDialog dialog )
	{
		if ( Active == dialog )
		{
			Active = null;
		}
	}

	public static bool IsActive()
	{
		return Active?.IsOpen ?? false;
	}

	public static void Close()
	{
		Active?.Close();
	}

	[Event.Client.BuildInput]
	private static void BuildInput()
	{
		if ( Active?.IsOpen ?? false )
		{
			Input.StopProcessing = true;
			Input.AnalogMove = Vector3.Zero;
		}
	}
}
