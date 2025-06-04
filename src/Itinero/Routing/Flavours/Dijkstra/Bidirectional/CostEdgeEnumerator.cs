using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;

internal class CostEdgeEnumerator
{
    private readonly RoutingNetworkEdgeEnumerator _edgeEnumerator;
    private readonly ICostFunction _costFunction;

    internal CostEdgeEnumerator(RoutingNetworkEdgeEnumerator edgeEnumerator, ICostFunction costFunction)
    {
        _edgeEnumerator = edgeEnumerator;
        _costFunction = costFunction;
    }

    public bool MoveTo(VertexId vertex)
    {
        return _edgeEnumerator.MoveTo(vertex);
    }

    public bool MoveTo(EdgeId edgeId, bool forward = true)
    {
        return _edgeEnumerator.MoveTo(edgeId, forward);
    }

    public (double cost, double turnCost) MoveToAndGetCost(EdgeId edgeId, bool forward, bool tailToHead,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        if (!_edgeEnumerator.MoveTo(edgeId, forward)) throw new Exception($"Edge not found!");

        return this.GetCost(tailToHead, previousEdges);
    }

    public (double cost, double turnCost) GetCost(bool tailToHead,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        return _costFunction.GetCost(_edgeEnumerator, tailToHead, previousEdges);
    }
}
