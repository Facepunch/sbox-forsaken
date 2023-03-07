namespace Facepunch.Forsaken.FlowFields.Grid
{
    public class GridConverter
    {
        private readonly GridDefinition GlobalDefinition;
        private readonly GridDefinition LocalDefinition;
        
        private readonly int LocalIndex;
        private readonly int GlobalIndex;

        public GridConverter( GridDefinition localDefinition, GridDefinition globalDefinition, int localIndex, int globalIndex )
        {
            LocalIndex = localIndex;
            GlobalIndex = globalIndex;
            LocalDefinition = localDefinition;
            GlobalDefinition = globalDefinition;
        }

        public int Global( int localIndex )
        {
            return ConvertIndex( LocalDefinition, LocalIndex, GlobalDefinition, GlobalIndex, localIndex );
        }

        private static Vector2i GetCoordinates( GridDefinition from, int fromPivot, GridDefinition to, int toPivot, int translateIndex )
        {
            var fromCoordinates = GridUtility.GetCoordinates( from, fromPivot );
            var toCoordinates = GridUtility.GetCoordinates( to, toPivot );

            var colDiff = fromCoordinates.x - toCoordinates.x;
            var rowDiff = fromCoordinates.y - toCoordinates.y;

            var coordinates = GridUtility.GetCoordinates( from, translateIndex );
            return new Vector2i( coordinates.x - colDiff, coordinates.y - rowDiff );
        }

        private static int ConvertIndex( GridDefinition from, int fromPivot, GridDefinition to, int toPivot, int translateIndex )
        {
            return ConvertIndex( to, GetCoordinates( from, fromPivot, to, toPivot, translateIndex ) );
        }

        private static int ConvertIndex( GridDefinition to, Vector2i coordinates )
        {
            return GridUtility.GetIndex( to, coordinates.y, coordinates.x );
        }
    }
}
