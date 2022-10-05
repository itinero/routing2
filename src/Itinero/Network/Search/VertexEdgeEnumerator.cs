using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Search;

internal class VertexEdgeEnumerator : IEdgeEnumerator<RoutingNetwork>
{
    private readonly IEnumerator<VertexId> _vertexEnumerator;

    public VertexEdgeEnumerator(RoutingNetwork graph, IEnumerable<VertexId> vertices)
    {
        this.Network = graph;
        _vertexEnumerator = vertices.GetEnumerator();
        this.RoutingNetworkEdgeEnumerator = graph.GetEdgeEnumerator();
    }

    private bool _firstEdge = false;

    public void Reset()
    {
        _firstEdge = false;
        this.RoutingNetworkEdgeEnumerator.Reset();
        _vertexEnumerator.Reset();
    }

    public bool MoveNext()
    {
        if (!_firstEdge)
        {
            while (_vertexEnumerator.MoveNext())
            {
                while (this.RoutingNetworkEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
                {
                    if (!this.RoutingNetworkEdgeEnumerator.MoveNext())
                    {
                        break;
                    }

                    _firstEdge = true;
                    return true;
                }
            }

            return false;
        }

        while (true)
        {
            if (this.RoutingNetworkEdgeEnumerator.MoveNext())
            {
                return true;
            }

            if (!_vertexEnumerator.MoveNext())
            {
                return false;
            }

            while (this.RoutingNetworkEdgeEnumerator.MoveTo(_vertexEnumerator.Current))
            {
                if (this.RoutingNetworkEdgeEnumerator.MoveNext())
                {
                    return true;
                }

                if (!_vertexEnumerator.MoveNext())
                {
                    return false;
                }
            }
        }
    }

    internal RoutingNetworkEdgeEnumerator RoutingNetworkEdgeEnumerator { get; }

    public void Dispose() { }

    public RoutingNetwork Network { get; }

    public bool Forward => this.RoutingNetworkEdgeEnumerator.Forward;
    public VertexId Tail => this.RoutingNetworkEdgeEnumerator.Tail;
    public (double longitude, double latitude, float? e) TailLocation => this.RoutingNetworkEdgeEnumerator.TailLocation;
    public VertexId Head => this.RoutingNetworkEdgeEnumerator.Head;
    public (double longitude, double latitude, float? e) HeadLocation => this.RoutingNetworkEdgeEnumerator.HeadLocation;
    public EdgeId EdgeId => this.RoutingNetworkEdgeEnumerator.EdgeId;
    public IEnumerable<(double longitude, double latitude, float? e)> Shape => this.RoutingNetworkEdgeEnumerator.Shape;
    public IEnumerable<(string key, string value)> Attributes => this.RoutingNetworkEdgeEnumerator.Attributes;
    public uint? EdgeTypeId => this.RoutingNetworkEdgeEnumerator.EdgeTypeId;
    public uint? Length => this.RoutingNetworkEdgeEnumerator.Length;
    public byte? HeadOrder => this.RoutingNetworkEdgeEnumerator.HeadOrder;
    public byte? TailOrder => this.RoutingNetworkEdgeEnumerator.TailOrder;

    /// <summary>
    /// Gets the turn cost at the tail turn (source -> [tail -> head]).
    /// </summary>
    /// <param name="sourceOrder">The order of the source edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToTail(
        byte sourceOrder)
    {
        return this.RoutingNetworkEdgeEnumerator.GetTurnCostToTail(sourceOrder);
    }

    /// <summary>
    /// Gets the turn cost at the tail turn ([head -> tail] -> target).
    /// </summary>
    /// <param name="targetOrder">The order of the target edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromTail(
        byte targetOrder)
    {
        return this.RoutingNetworkEdgeEnumerator.GetTurnCostFromTail(targetOrder);
    }

    /// <summary>
    /// Gets the turn cost at the tail turn (source -> [head -> tail]).
    /// </summary>
    /// <param name="sourceOrder">The order of the source edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostToHead(
        byte sourceOrder)
    {
        return this.RoutingNetworkEdgeEnumerator.GetTurnCostToHead(sourceOrder);
    }

    /// <summary>
    /// Gets the turn cost at the tail turn ([tail -> head] -> target).
    /// </summary>
    /// <param name="targetOrder">The order of the target edge.</param>
    /// <returns>The turn costs if any.</returns>
    public IEnumerable<(uint turnCostType, IEnumerable<(string key, string value)> attributes, uint cost, IEnumerable<EdgeId> prefixEdges)> GetTurnCostFromHead(
        byte targetOrder)
    {
        return this.RoutingNetworkEdgeEnumerator.GetTurnCostFromHead(targetOrder);
    }
}
