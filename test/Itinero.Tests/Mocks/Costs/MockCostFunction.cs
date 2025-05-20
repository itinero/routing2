using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Tests.Mocks.Costs;

public class MockCostFunction : ICostFunction
{
    private readonly Func<EdgeId, double> _forward;
    private readonly Func<EdgeId, double> _backward;

    private MockCostFunction(Func<EdgeId, double> backward, Func<EdgeId, double> forward)
    {
        _forward = forward;
        _backward = backward;
    }

    public static MockCostFunction Create(double cost)
    {
        return Create(cost, cost);
    }

    public static MockCostFunction Create(double backwardCost, double forwardCost)
    {
        return new MockCostFunction((_) => backwardCost, (_) => forwardCost);
    }

    public (bool canAccess, bool canStop, double cost, double turnCost) Get(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        double c;
        if (tailToHead)
        {
            c = edgeEnumerator.Forward ? _forward(edgeEnumerator.EdgeId) : _backward(edgeEnumerator.EdgeId);
        }
        else
        {
            c = edgeEnumerator.Forward ? _backward(edgeEnumerator.EdgeId) : _forward(edgeEnumerator.EdgeId);
        }

        return (c > 0, true, c, 0);
    }
}
