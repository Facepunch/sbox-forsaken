using System.Collections.Generic;
using System.Linq;
using Facepunch.Forsaken.FlowFields.Grid;

namespace Facepunch.Forsaken.FlowFields.Algorithms
{
    public sealed class AStarGateway
    {
		public static AStarGateway Default => InternalDefault ?? (InternalDefault = new());
		private static AStarGateway InternalDefault;

        private readonly HashSet<int> ClosedSet = new();
        private readonly HashSetList<int> OpenSet = new();
        private readonly Dictionary<int, int> Previous = new();
        private GridDefinition Definition;

        private List<int> End;

        private int[] _f;
        private int[] _g;

        private List<int> ReconstructPath( int current )
        {
            var path = new List<int>();

            while  ( Previous.ContainsKey( current ) )
            {
                path.Add( current );

                if ( Previous[current] == current )
					break;

                current = Previous[current];
            }

            return path;
        }

        public static int GetPathCost( IEnumerable<int> nodes, byte[] costs )
        {
            return nodes.Aggregate( 0, (current, node) => current + costs[node] );
        }

        public List<int> GetPath( GridDefinition definition, byte[] costs, int start, int end )
        {
            var ends = new List<int> { end };
            return GetPath( definition, costs, start, ends );
        }

		public List<int> GetPath( GridDefinition definition, byte[] costs, int start, List<int> end )
        {
            End = end;
            Definition = definition;

            _f = new int[definition.Size];
            _g = new int[definition.Size];

            for ( var i = 0; i < _g.Length; i++ )
            {
                _g[i] = int.MaxValue;
                _f[i] = int.MaxValue;
            }

            _g[start] = 0;
            _f[start] = H( start );

            ClosedSet.Clear();
            Previous.Clear();
            OpenSet.Clear();

			if ( end.Contains( start ) )
			{
				return new List<int> { start };
			}

            OpenSet.Add(start);

            while ( OpenSet.Count > 0 )
            {
                OpenSet.Sort( CompareValues );

				var current = OpenSet[0];

                if ( end.Contains( current ) )
				{
					return ReconstructPath( current );
				}

                OpenSet.Remove( current );
                ClosedSet.Add( current );

                foreach ( var neighborItem in GridUtility.GetNeighborsIndex( current, definition, true ) )
                {
                    var neighbor = neighborItem.Value;

                    if ( ClosedSet.Contains( neighbor ) )
                        continue;

                    if ( OpenSet.Contains( neighbor ) )
                        continue;

                    if ( !GridUtility.IsValid( neighbor ) || costs[neighbor] == Chunk.Impassable )
                    {
                        ClosedSet.Add( neighbor );
                        continue;
                    }

                    var tentativeGScore = _g[current] + costs[neighbor] + D();
                    if (tentativeGScore >= _g[neighbor]) continue;

                    Previous[neighbor] = current;
                    _g[neighbor] = tentativeGScore;
                    _f[neighbor] = _g[neighbor] + H( neighbor );
                    OpenSet.Add( neighbor );
                }
            }

			return null;
        }

		private int CompareValues( int a, int b )
		{
			return _f[a].CompareTo( _f[b] );
		}

        private int H( int i )
        {
            return GridUtility.Distance( Definition, i, End[0] );
        }

        private static int D()
        {
            return 1;
        }
    }
}
