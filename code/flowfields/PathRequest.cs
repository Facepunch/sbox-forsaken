using Sandbox;

namespace Facepunch.Forsaken.FlowFields
{
    public class PathRequest : IValid
	{
		public FlowField FlowField;

		public bool IsValid => FlowField != null;

		public Vector3 GetDirection( Vector3 position )
		{
			if ( !IsValid || FlowField.Ready( position ) != PathResult.Valid )
				return Vector3.Zero;

			return FlowField.GetDirection( position );
		}

		public bool IsDestination( Vector3 position )
		{
			// We've reached our destination if we don't have one.
			if ( !FlowField.HasDestination() )
				return true;

			// We've reached our destination if the path is invalid.
			if ( FlowField.Ready( position ) == PathResult.Invalid )
				return true;

			var indicies = FlowField.DestinationIndexes;
			var pathfinder = FlowField.Pathfinder;
			var worldPosition = pathfinder.CreateWorldPosition( position );

			return indicies.Contains( worldPosition.WorldIndex );
		}

		public Vector3 GetDestination()
		{
			return FlowField.DestinationPosition;
		}

		public bool HasDestination() => FlowField.HasDestination();
    }
}
