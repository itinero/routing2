using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Routing.Costs;

internal class AlternativeRouteCostFunction : ICostFunction
{
    private readonly ICostFunction _originalCostFunction;
    private readonly Dictionary<EdgeId, int> _moreCostlyEdges;
    private readonly double _alreadyVisitedCostFactor;

    /// <summary>
    /// Calculates the cost of the edge as specified by the originalCostFunction.
    /// If the edge is in moreCostlyEdges, the cost is increased.
    /// </summary>
    /// <param name="originalCostFunction"></param>
    /// <param name="moreCostlyEdges"></param>
    /// <param name="alreadyVisitedCostFactor"></param>
    public AlternativeRouteCostFunction(ICostFunction originalCostFunction, Dictionary<EdgeId, int> moreCostlyEdges, double alreadyVisitedCostFactor = 2.0)
    {
        _originalCostFunction = originalCostFunction;
        _moreCostlyEdges = moreCostlyEdges;
        _alreadyVisitedCostFactor = alreadyVisitedCostFactor;
    }

    public (bool canAccess, bool canStop, double cost, double turnCost) Get(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        previousEdges ??= ArraySegment<(EdgeId edgeId, byte? turn)>.Empty;

        if (_moreCostlyEdges.TryGetValue(edgeEnumerator.EdgeId, out var count))
        {
            var alreadyVisitedCost = Math.Pow(_alreadyVisitedCostFactor, count);

            var (canAccess, canStop, cost, turnCost) = _originalCostFunction.Get(edgeEnumerator, tailToHead, previousEdges);
            return (canAccess, canStop, cost * alreadyVisitedCost, turnCost);
        }

        return _originalCostFunction.Get(edgeEnumerator, tailToHead, previousEdges);
    }
}
