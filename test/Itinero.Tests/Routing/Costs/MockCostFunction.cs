using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Tests.Routing.Costs;

public class MockCostFunction : ICostFunction
{
    private readonly Func<EdgeId, double> _mockFunc;

    public MockCostFunction(Func<EdgeId, double> mockFunc)
    {
        _mockFunc = mockFunc;
    }

    public (bool canAccess, bool canStop, double cost, double turnCost) Get(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool forward,
        IEnumerable<(EdgeId edgeId, byte? turn)> previousEdges)
    {
        return (true, true, _mockFunc(edgeEnumerator.EdgeId), 0);
    }
}
