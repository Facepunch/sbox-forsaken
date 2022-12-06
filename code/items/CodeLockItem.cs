using System.Collections.Generic;

namespace Facepunch.Forsaken;

public class CodeLockItem : InventoryItem
{
	public override string Description => "Apply a code to doors so that other people can open them.";
	public override string UniqueId => "code_lock";
	public override string Name => "Code Lock";
	public override string Icon => "textures/items/code_lock.png";

	public override bool CanStackWith( InventoryItem other )
	{
		return false;
	}
}
