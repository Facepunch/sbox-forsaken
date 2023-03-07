using System.Collections.Generic;
using Facepunch.Forsaken.FlowFields.Grid;

namespace Facepunch.Forsaken.FlowFields.Algorithms
{
    public class Integration : IIntegration
    {
        public const int NoIndex = -1;
        public bool IsIntegrated;

        private readonly List<int> OpenIndices = new();
        private readonly IntegrationSort Sorter;
        private readonly int[] Values;

        public Integration( GridDefinition definition )
        {
            Definition = definition;
            Values = new int[Definition.Size];
            Sorter = new IntegrationSort( this );
            Reset();
        }

        public GridDefinition Definition { get; }

        public int GetValue( int index )
        {
            return Values[index];
        }

        private void Reset()
        {
            for ( var i = 0; i < Definition.Size; i++ )
                Values[i] = IntegrationService.UnIntegrated;

            OpenIndices.Clear();
            Sorter.Reset();
        }

        public void SetValue( int index, int value )
        {
            Values[index] = value;
        }

        public void Enqueue( int index )
        {
            OpenIndices.Add( index );
            OpenIndices.Sort( (index1, index2) => Values[index1].CompareTo( Values[index2] ) );
        }

        public int Dequeue()
        {
            if (OpenIndices.Count == 0) return NoIndex;
            
            var index = OpenIndices[0];
			OpenIndices.RemoveAt(0);
            return index;
        }
    }
}
