using System.Collections.Generic;

namespace Facepunch.Forsaken.FlowFields.Algorithms
{
    public class IntegrationSort
    {
        private readonly IIntegration Integration;

        private readonly Queue<int> OpenIndices = new();
        private readonly Queue<int> SortIndices = new();
        private int SortTopIndex;
        private int TopIndex;

        public IntegrationSort( IIntegration integration )
        {
            Integration = integration;
        }

        public void Reset()
        {
            TopIndex = 0;
            SortTopIndex = 0;
            OpenIndices.Clear();
            SortIndices.Clear();
        }

        public void Enqueue( int index )
        {
            var value = Integration.GetValue( index );

            if (TopIndex == 0)
                TopIndex = value;

            if (TopIndex > value)
            {
                TopIndex = value;

                while ( OpenIndices.Count > 0 )
					SortIndices.Enqueue( OpenIndices.Dequeue() );
            }

            if ( TopIndex + 1 >= Integration.GetValue( index ) )
            {
                OpenIndices.Enqueue(index);
            }
            else
            {
                if ( SortTopIndex == 0 )
                    SortTopIndex = Integration.GetValue( index );

                SortIndices.Enqueue(index);
            }
        }

        public int Dequeue()
        {
            if ( SortTopIndex < TopIndex && SortIndices.Count > 0 )
            {
                var index = SortIndices.Dequeue();
                SortTopIndex = Integration.GetValue(index);
                return index;
            }

            if ( OpenIndices.Count > 0 )
            {
                var index = OpenIndices.Dequeue();
                TopIndex = Integration.GetValue(index);
                return index;
            }

            if ( SortIndices.Count > 0 )
            {
                var index = SortIndices.Dequeue();
                SortTopIndex = Integration.GetValue(index);
                return index;
            }

            return Algorithms.Integration.NoIndex;
        }

        private int PeekValue()
        {
            if ( SortTopIndex < TopIndex && SortIndices.Count > 0 ) return Integration.GetValue( SortIndices.Peek() );
            if ( OpenIndices.Count > 0 ) return Integration.GetValue( OpenIndices.Peek() );
            if ( SortIndices.Count > 0 ) return Integration.GetValue( SortIndices.Peek() );

            return Algorithms.Integration.NoIndex;
        }

        public void Dequeue( Queue<int> indexes, int value )
        {
            while ( PeekValue() == value )
                indexes.Enqueue(Dequeue());
        }
    }
}
