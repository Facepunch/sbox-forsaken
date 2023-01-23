
using System.Collections.Generic;

namespace Facepunch.Forsaken;

public interface IRecyclableItem
{
	public Dictionary<string,int> RecycleOutput { get; }
	public float BaseComponentReturn { get; }
	public bool IsRecyclable { get; }
	public string UniqueId { get; }
}
