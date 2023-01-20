using Sandbox;

namespace Facepunch.Forsaken;

public interface INametagProvider : IValid
{
	public Color? NametagColor { get; }
	public string DisplayName { get; }
	public bool IsInactive { get; }
	public bool ShowNametag { get; }
	public Vector3 EyePosition { get; }
	public Rotation Rotation { get; }
}
