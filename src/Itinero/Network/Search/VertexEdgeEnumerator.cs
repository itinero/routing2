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
            if (!_firstEdge) {
                while (_vertexEnumerator.MoveNext()) {
                    while (RoutingNetworkEdgeEnumerator.MoveTo(_vertexEnumerator.Current)) {
                        if (!RoutingNetworkEdgeEnumerator.MoveNext()) {
                            break;
                        }

                        _firstEdge = true;
                        return true;
                    }
                }

                return false;
            }

            while (true) {
                if (RoutingNetworkEdgeEnumerator.MoveNext()) {
                    return true;
                }

                if (!_vertexEnumerator.MoveNext()) {
                    return false;
                }

                while (RoutingNetworkEdgeEnumerator.MoveTo(_vertexEnumerator.Current)) {
                    if (RoutingNetworkEdgeEnumerator.MoveNext()) {
                        return true;
                    }

                    if (!_vertexEnumerator.MoveNext()) {
                        return false;
                    }
                }
            }
        }

        internal RoutingNetworkEdgeEnumerator RoutingNetworkEdgeEnumerator { get; }

        public void Dispose() { }

        public RoutingNetwork Network { get; }

        public bool Forward => RoutingNetworkEdgeEnumerator.Forward;
        public VertexId Tail => RoutingNetworkEdgeEnumerator.Tail;
        public (double longitude, double latitude, float? e) TailLocation => RoutingNetworkEdgeEnumerator.TailLocation;
        public VertexId Head => RoutingNetworkEdgeEnumerator.Head;
        public (double longitude, double latitude, float? e) HeadLocation => RoutingNetworkEdgeEnumerator.HeadLocation;
        public EdgeId EdgeId => RoutingNetworkEdgeEnumerator.EdgeId;
        public IEnumerable<(double longitude, double latitude, float? e)> Shape => RoutingNetworkEdgeEnumerator.Shape;
        public IEnumerable<(string key, string value)> Attributes => RoutingNetworkEdgeEnumerator.Attributes;
        public uint? EdgeTypeId => RoutingNetworkEdgeEnumerator.EdgeTypeId;
        public uint? Length => RoutingNetworkEdgeEnumerator.Length;
        public byte? HeadOrder => RoutingNetworkEdgeEnumerator.HeadOrder;
        public byte? TailOrder => RoutingNetworkEdgeEnumerator.TailOrder;

        /// <summary>
        /// Gets the turn cost at the tail turn (source -> [tail -> head]).
        /// </summary>
        /// <param name="sourceOrder">The order of the source edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToTail(
            byte sourceOrder)
        {
            return RoutingNetworkEdgeEnumerator.GetTurnCostToTail(sourceOrder);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn ([head -> tail] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromTail(
            byte targetOrder)
        {
            return RoutingNetworkEdgeEnumerator.GetTurnCostFromTail(targetOrder);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn (source -> [head -> tail]).
        /// </summary>
        /// <param name="sourceOrder">The order of the source edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToHead(
            byte sourceOrder)
        {
            return RoutingNetworkEdgeEnumerator.GetTurnCostToHead(sourceOrder);
        }

        /// <summary>
        /// Gets the turn cost at the tail turn ([tail -> head] -> target).
        /// </summary>
        /// <param name="targetOrder">The order of the target edge.</param>
        /// <returns>The turn costs if any.</returns>
        public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromHead(
            byte targetOrder)
        {
            return RoutingNetworkEdgeEnumerator.GetTurnCostFromHead(targetOrder);
        }
    }
}