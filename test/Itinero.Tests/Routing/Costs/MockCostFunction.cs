using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Tests.Routing.Costs;

public class MockCostFunction : ICostFunction
{
    private readonly Func<EdgeId, double> _mockFunc;

    public MockCostFunction(Func<EdgeId, double> mockFunc)
    {
        _mockFunc = mockFunc;
    }

    public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        PreviousEdgeEnumerable previousEdges = default)
    {

        return (true, true, false, _mockFunc(edgeEnumerator.EdgeId), 0);
    }
}
