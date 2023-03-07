using System;
using Sandbox;

namespace Facepunch.Forsaken.FlowFields.Grid
{
    public class GridDefinition
    {
        public static GridDefinition Zero = new();

        public int Rows;
        public int Columns;

        public GridDefinition()
        {
        }

        public GridDefinition( Vector2i size )
        {
            Rows = size.y;
            Columns = size.x;
        }

        public GridDefinition( int rows, int columns )
        {
            Rows = rows;
            Columns = columns;
        }

        public int Size => Rows * Columns;

        public GridDefinition Scaled( long scale )
        {
            return new GridDefinition( ((float)Rows / scale).CeilToInt(), ((float)Columns / scale).CeilToInt() );
        }

        public int GetIndex( int row, int column )
        {
            return row * Columns + column;
        }

        public Vector2i GetCoordinates( int index )
        {
            return index == 0 ? Vector2i.Zero : new Vector2i( index % Columns, index / Columns );
        }
    }
}
