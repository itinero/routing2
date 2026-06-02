using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Tests.Mocks.Costs;

public class MockCostFunction : ICostFunction
{
    private readonly Func<EdgeId, double> _forward;
    private readonly Func<EdgeId, double> _backward;
    private readonly HashSet<VertexId> _barriers;

    private MockCostFunction(Func<EdgeId, double> backward, Func<EdgeId, double> forward, HashSet<VertexId> barriers)
    {
        _forward = forward;
        _backward = backward;
        _barriers = barriers;
    }

    public static MockCostFunction Create(double cost)
    {
        return Create(cost, cost);
    }

    public static MockCostFunction Create(double cost, HashSet<VertexId> barriers)
    {
        return Create(cost, cost, barriers);
    }

    public static MockCostFunction Create(double backwardCost, double forwardCost)
    {
        return Create(backwardCost, forwardCost, []);
    }

    public static MockCostFunction Create(double backwardCost, double forwardCost, HashSet<VertexId> barriers)
    {
        return new MockCostFunction((_) => backwardCost, (_) => forwardCost, barriers);
    }

    public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        var turnCost = 0.0;
        if (_barriers.Contains(edgeEnumerator.Tail))
        {
            turnCost = double.MaxValue;
        }

        double c;
        if (tailToHead)
        {
            c = edgeEnumerator.Forward ? _forward(edgeEnumerator.EdgeId) : _backward(edgeEnumerator.EdgeId);
        }
        else
        {
            c = edgeEnumerator.Forward ? _backward(edgeEnumerator.EdgeId) : _forward(edgeEnumerator.EdgeId);
        }

        return (c > 0, true, false, c, turnCost);
    }
}
