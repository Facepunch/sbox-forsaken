using System.Collections.Generic;
using Facepunch.Forsaken.FlowFields.Grid;
using Facepunch.Forsaken.FlowFields.Connectors;

namespace Facepunch.Forsaken.FlowFields.Algorithms
{
    public sealed class AStarPortal
    {
		public static AStarPortal Default => InternalDefault ?? (InternalDefault = new());
		private static AStarPortal InternalDefault;

        private readonly Dictionary<Portal, long> _f = new();
        private readonly Dictionary<Portal, long> _g = new();

        private readonly HashSetList<Gateway> OpenSet = new();
        private readonly HashSet<Gateway> ClosedSet = new();
        
        private readonly Dictionary<Gateway, Gateway> Previous = new();

        private GatewaySubPath ReconstructPath( FlowField field, Gateway current )
        {
            var subPath = new GatewaySubPath {UntilConnectionGateway = current};

            while ( Previous.ContainsKey(current) )
            {
                if ( field.GatewayPath.ContainsKey( Previous[current]) )
                    break;

                field.GatewayPath.Add( Previous[current], current );

                if ( Previous[current].Equals( current ) )
                    break;

                current = Previous[current];
            }

            subPath.FromConnectionGateway = current;

            return subPath;
        }

        private int H( Pathfinder pathfinder, Gateway gateway, int worldIndex )
        {
            var from = pathfinder.CreateWorldPosition( gateway.Chunk, gateway.Median() );
            var to = worldIndex;
            return GridUtility.Distance( pathfinder.WorldGridSize, from.WorldIndex, to );
        }

        public GatewaySubPath CalculatePath( FlowField field, Vector3 startPosition )
        {
            SetupPath( field );
            return MergePathFrom( field, startPosition );
        }

        private void SetupPath( FlowField field )
        {
            Previous.Clear();
        }

        private GatewaySubPath MergePathFrom( FlowField field, Vector3 startPosition )
        {
            var worldPosition = field.Pathfinder.CreateWorldPosition( startPosition );

			ClosedSet.Clear();
            OpenSet.Clear();

            var chunk = field.Pathfinder.GetChunk( worldPosition.ChunkIndex );
            if ( chunk == null ) return null;

            var portals = chunk.GetConnectedPortals( worldPosition.NodeIndex );

			for ( int i = 0; i < portals.Count; i++ )
			{
				var portal = portals[i];
				var gateway = portal.GetGatewayInChunk( chunk );

				OpenSet.Add( gateway );
			}

			_f.Clear();
            _g.Clear();

			for ( int i = 0; i < field.Pathfinder.Portals.Count; i++ )
            {
				var portal = field.Pathfinder.Portals[i];
				_f.Add( portal, long.MaxValue );
                _g.Add( portal, long.MaxValue );
            }

			for ( int i = 0; i < OpenSet.Count; i++ )
            {
				var openGateway = OpenSet[i];
				_g[openGateway.Portal] = 0;
                _f[openGateway.Portal] = H( field.Pathfinder, openGateway, field.DestinationIndex );
            }

            while ( OpenSet.Count > 0 )
            {
                OpenSet.Sort( (index1, index2) => _f[index1.Portal].CompareTo( _f[index2.Portal] ) );

				var current = OpenSet[0];
                var oppositeGateway = current.Portal.OppositeGateway(current);

                if ( field.GatewayPath.ContainsKey( current ) )
					return ReconstructPath( field, current );

                if ( field.DestinationGateways.ContainsKey( oppositeGateway.Chunk ) &&
                    field.DestinationGateways[oppositeGateway.Chunk].Contains( oppositeGateway ))
                {
                    Previous[oppositeGateway] = current;
                    return ReconstructPath( field, oppositeGateway );
                }

                OpenSet.Remove( current );
                ClosedSet.Add( current );

                AddGatewayConnections( field, current );
            }

            return null;
        }

        private void AddGatewayConnections( FlowField field, Gateway connectionGateway )
        {
            var opposite = connectionGateway.Portal.OppositeGateway( connectionGateway );

            foreach ( var connection in opposite.Connections )
            {
                var connectedGateway = connection.Key;

                if ( ClosedSet.Contains( connectedGateway ) )
                    continue;

                if ( OpenSet.Contains( connectedGateway ) )
                    continue;

                var tentativeGScore = _g[connectionGateway.Portal] + connection.Value;
                if ( tentativeGScore >= _g[connectedGateway.Portal] ) continue;

                Previous[connectedGateway] = connectionGateway;
                _g[connectedGateway.Portal] = tentativeGScore;
                _f[connectedGateway.Portal] = _g[connectedGateway.Portal] + H( field.Pathfinder, connectedGateway, field.DestinationIndex );

                OpenSet.Add( connectedGateway );
            }
        }
    }
}
