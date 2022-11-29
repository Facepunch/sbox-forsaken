using Sandbox;

namespace Facepunch.Forsaken;

public static class EntityExtension
{
	public static T FindParentOfType<T>( this Entity self ) where T : class
	{
		if ( self is T ) return self as T;

		Entity parent = null;

		while ( parent is null && self.Parent.IsValid() && !self.Parent.IsWorld )
		{
			if ( self.Parent is T )
				return self.Parent as T;

			parent = self.Parent;
		}

		return default;
	}
}
