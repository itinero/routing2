using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Search
{
    internal class VertexEdgeEnumerator : IEdgeEnumerator<RoutingNetwork>
    {
        private readonly IEnumerator<VertexId> _vertexEnumerator;

        public VertexEdgeEnumerator(RoutingNetwork graph, IEnumerable<VertexId> vertices)
        {
            Network = graph;
            _vertexEnumerator = vertices.GetEnumerator();
            RoutingNetworkEdgeEnumerator = graph.GetEdgeEnumerator();
        }
        
        private bool _firstEdge = false;
        
        public void Reset()
        {
            _firstEdge = false;
            RoutingNetworkEdgeEnumerator.Reset();
            _vertexEnumerator.Reset();
        }

        public bool MoveNext()
        {
            if (!_firstEdge)
            {
                while (_vertexEnumerator.MoveNext())
                {
                    while (RoutingNetworkEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
                    {
                        if (!RoutingNetworkEdgeEnumerator.MoveNext()) break;

                        _firstEdge = true;
                        return true;
                    }
                }

                return false;
            }

            while (true)
            {
                if (RoutingNetworkEdgeEnumerator.MoveNext())
                {
                    return true;
                }

                if (!_vertexEnumerator.MoveNext()) return false;
                while (RoutingNetworkEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
                {
                    if (RoutingNetworkEdgeEnumerator.MoveNext()) return true;
                    if (!_vertexEnumerator.MoveNext()) return false;
                }
            }
        }

        internal RoutingNetworkEdgeEnumerator RoutingNetworkEdgeEnumerator { get; }

        public void Dispose()
        {
            
        }

        public RoutingNetwork Network { get; }

        public bool Forward => RoutingNetworkEdgeEnumerator.Forward;
        public VertexId From => RoutingNetworkEdgeEnumerator.From;
        public (double longitude, double latitude) FromLocation => RoutingNetworkEdgeEnumerator.FromLocation;
        public VertexId To => RoutingNetworkEdgeEnumerator.To;
        public (double longitude, double latitude) ToLocation => RoutingNetworkEdgeEnumerator.ToLocation;
        public EdgeId Id => RoutingNetworkEdgeEnumerator.Id;
        public IEnumerable<(double longitude, double latitude)> Shape => RoutingNetworkEdgeEnumerator.Shape;
        public IEnumerable<(string key, string value)> Attributes => RoutingNetworkEdgeEnumerator.Attributes;
        public uint? EdgeTypeId => RoutingNetworkEdgeEnumerator.EdgeTypeId;
        public uint? Length => RoutingNetworkEdgeEnumerator.Length;
        public byte? Head => RoutingNetworkEdgeEnumerator.Head;
        public byte? Tail => RoutingNetworkEdgeEnumerator.Tail;
        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostTo(byte fromOrder)
        {
            return RoutingNetworkEdgeEnumerator.GetTurnCostTo(fromOrder);
        }

        public IEnumerable<(uint turnCostType, uint cost)> GetTurnCostFrom(byte toOrder)
        {
            return RoutingNetworkEdgeEnumerator.GetTurnCostFrom(toOrder);
        }
    }
}