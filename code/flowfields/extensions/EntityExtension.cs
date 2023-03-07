using System;
using System.Collections.Generic;
using Sandbox;

namespace Facepunch.Forsaken.FlowFields.Extensions
{
	public static class EntityExtension
	{
		public static List<Vector3> GetDestinations( this ModelEntity self, Pathfinder pathfinder, bool sortByDistance = false )
		{
			var collisionSize = pathfinder.CollisionSize;
			var nodeSize = pathfinder.NodeSize;
			var diameter = MathF.Max( self.CollisionBounds.Size.x, self.CollisionBounds.Size.y ) * 0.6f;

			// Round up the radius to the nearest node size.
			var radius = MathF.Ceiling( (diameter + collisionSize / 2f) / nodeSize ) * nodeSize;
			var locations = new List<Vector3>();

			pathfinder.GetGridPositions( self.Position, radius, locations, true );

			if ( sortByDistance )
			{
				locations.Sort( ( a, b ) => a.Distance( self.Position ).CompareTo( b.Distance( self.Position ) ) );
			}

			return locations;
		}
	}
}
