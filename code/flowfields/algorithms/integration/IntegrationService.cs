using System;
using System.Collections.Generic;
using Facepunch.Forsaken.FlowFields.Grid;
using Sandbox;

namespace Facepunch.Forsaken.FlowFields.Algorithms
{
    public abstract class IntegrationService
    {
        public const int UnIntegrated = int.MaxValue;
        public const int Closed = int.MinValue;

        private readonly List<GridNeighbor> NeighborIndices = new( 8 );

        public static Integration CreateIntegration( GridDefinition definition )
        {
            return new Integration( definition );
        }

        public void Integrate( FlowField flowField, Integration integration, int integrationIndex )
        {
            while ( true )
            {
                var index = integration.Dequeue();

                if ( index == Integration.NoIndex )
                    break;

                NeighborIndices.Clear();

                var currentIntegrationValue = integration.GetValue( index );
                var chunk = flowField.Pathfinder.GetChunk( integrationIndex );

                GridUtility.GetNeighborsIndexNonAlloc( index, integration.Definition, NeighborIndices, true );

                foreach ( var neighbor in NeighborIndices )
                {
                    var neighborIndex = neighbor.Index;

                    if ( !GridUtility.IsValid( neighborIndex ) )
                        continue;

					if ( integration.GetValue( neighborIndex ) != UnIntegrated )
						continue;

					if ( !flowField.IsAvailable( integrationIndex, neighborIndex ) )
                    {
						integration.SetValue( neighborIndex, -currentIntegrationValue );
						continue;
                    }

					if ( currentIntegrationValue < 0 )
                        continue;

                    var value = H( currentIntegrationValue, chunk.GetCost( index ) );

                    if ( neighbor.Direction == GridDirection.RightDown ||
                        neighbor.Direction == GridDirection.LeftUp ||
                        neighbor.Direction == GridDirection.DownLeft ||
                        neighbor.Direction == GridDirection.UpRight )
					{
                        value += 1;
					}

                    integration.SetValue( neighborIndex, value );
                    integration.Enqueue( neighborIndex );
                }
            }
        }

        public static int H( int integration, int cost = 0 )
        {
            return integration + 1 + cost;
        }
    }
}
