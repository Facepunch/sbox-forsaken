using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Forsaken.FlowFields.Algorithms;
using Facepunch.Forsaken.FlowFields.Grid;
using Facepunch.Forsaken.FlowFields.Connectors;
using Sandbox;

namespace Facepunch.Forsaken.FlowFields
{
    public class Chunk
    {
        public const byte Impassable = byte.MaxValue;

		private List<Portal> ConnectedPortals = new();
		private List<Gateway> Gateways = new();
        private readonly NodeCollision[] InternalCollisions;
		private readonly byte[] Costs;
		private readonly GridDefinition InternalDefinition;
		private readonly int InternalIndex;
		private bool IsDivided;

		public NodeCollision[] Collisions => InternalCollisions;
        public GridDefinition Definition => InternalDefinition;
        public int Size => InternalDefinition.Size;
        public int Index => InternalIndex;

        public Chunk( int index, GridDefinition definition )
        {
            InternalDefinition = definition;
            InternalIndex = index;
            Costs = new byte[InternalDefinition.Size];
            InternalCollisions = new NodeCollision[InternalDefinition.Size];
        }

        public static implicit operator int( Chunk chunk )
        {
            return chunk.InternalIndex;
        }

        public bool IsImpassable( int index )
        {
            return GetCost( index ) == Impassable || (InternalCollisions[index] != NodeCollision.None);
        }

        public bool HasCollision( int index )
        {
            return (InternalCollisions[index] != NodeCollision.None);
        }

		public NodeCollision GetCollision( int index )
		{
			return InternalCollisions[index];
		}

        public void ClearCollisions()
        {
			Array.Clear( InternalCollisions, 0, InternalCollisions.Length );
        }

        public void SetCollision( int index, NodeCollision type = NodeCollision.Dynamic )
        {
            InternalCollisions[index] = type;
            Costs[index] = Impassable;
        }

        public void RemoveCollision( int index )
        {
            InternalCollisions[index] = NodeCollision.None;
            Costs[index] = 0;
        }

        public int GetCost( int index )
        {
            return (InternalCollisions[index] != NodeCollision.None) ? Impassable : GetRawCost(index);
        }

        public int GetRawCost( int index )
        {
            return Costs[index];
        }

        public void SetCost( int index, byte cost )
        {
            Costs[index] = cost;
        }

        public void IncrementCost( int index )
        {
            Costs[index] = (byte)Math.Clamp( Costs[index] + 10, byte.MinValue, byte.MaxValue );
        }

        public void DecrementCost( int index )
        {
            Costs[index] = (byte)Math.Clamp( Costs[index] - 10, byte.MinValue, byte.MaxValue );
        }

        public void ClearGateways( GridDirection direction = GridDirection.Zero )
        {
			if ( Gateways == null )
				Gateways = new();

            if (direction == GridDirection.Zero)
                Gateways.Clear();
            else
                Gateways.RemoveAll( gateway => gateway.Direction == direction );
        }

        public void AddGateway( Gateway connectionGateway )
        {
            Gateways.Add( connectionGateway );
        }

        public bool IsInitialized()
        {
            return Gateways != null;
        }

        public bool HasGateway( int index )
        {
            return Gateways.Any( gateway => gateway.Contains(index) );
        }

        public void ConnectGateways()
        {
            IsDivided = false;

            foreach ( var gateway in Gateways )
			{
                gateway.Connections.Clear();
			}

            for ( var i = 0; i < Gateways.Count; i++ )
            for ( var j = i + 1; j < Gateways.Count; j++ )
            {
                var gateway1 = Gateways[i];
                var gateway2 = Gateways[j];

                var path = AStarGateway.Default.GetPath(
                    InternalDefinition,
                    Costs,
                    gateway1.Median(),
                    gateway2.Median()
                );

                if ( path == null )
                {
                    IsDivided = true;
                    continue;
                }

                var cost = AStarGateway.GetPathCost( path, Costs );

                if ( !gateway1.Connections.ContainsKey( gateway2 ) )
                    gateway1.Connections.Add( gateway2, cost );
                if ( !gateway2.Connections.ContainsKey( gateway1 ) )
                    gateway2.Connections.Add( gateway1, cost );
            }
        }

        public bool Connects( Gateway gateway, List<int> nodes )
        {
            return AStarGateway.Default.GetPath( InternalDefinition, Costs, gateway.Median(), nodes[0] ) != null;
        }

        public List<Gateway> GetGateways()
        {
            return Gateways.ToList();
        }

        public List<Gateway> GetGatewaysToChunk( int index )
        {
            return Gateways.Where( gateway => gateway.Portal.HasChunk(index) ).ToList();
        }

        public List<Portal> GetConnectedPortals( int index )
        {
			ConnectedPortals ??= new();
			ConnectedPortals.Clear();

            if ( IsDivided )
                ConnectedPortals.AddRange( from gateway in Gateways
                    where AStarGateway.Default.GetPath( InternalDefinition, Costs, gateway.Median(), index ) != null
                    select gateway.Portal );
            else
                ConnectedPortals.AddRange( Gateways.Select( gateway => gateway.Portal ) );

            return ConnectedPortals;
        }
    }
}
