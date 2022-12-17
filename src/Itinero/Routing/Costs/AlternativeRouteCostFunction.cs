using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Routing.Costs;

internal class AlternativeRouteCostFunction : ICostFunction
{
    private readonly ICostFunction _originalCostFunction;
    private readonly HashSet<EdgeId> _moreCostlyEdges;
    private readonly double _alreadyVisitedCostFactor;

    /// <summary>
    /// Calculates the cost of the edge as specified by the originalCostFunction.
    /// If the edge is in moreCostlyEdges, the cost is increased.
    /// </summary>
    /// <param name="originalCostFunction"></param>
    /// <param name="moreCostlyEdges"></param>
    /// <param name="alreadyVisitedCostFactor"></param>
    public AlternativeRouteCostFunction(ICostFunction originalCostFunction, HashSet<EdgeId> moreCostlyEdges, double alreadyVisitedCostFactor = 2.0)
    {
        _originalCostFunction = originalCostFunction;
        _moreCostlyEdges = moreCostlyEdges;
        _alreadyVisitedCostFactor = alreadyVisitedCostFactor;
    }

    public (bool canAccess, bool canStop, double cost, double turnCost) Get(IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool forward = true,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        previousEdges ??= ArraySegment<(EdgeId edgeId, byte? turn)>.Empty;

        if (_moreCostlyEdges.Contains(edgeEnumerator.EdgeId))
        {
            var (canAccess, canStop, cost, turnCost) = _originalCostFunction.Get(edgeEnumerator, forward, previousEdges);
            return (canAccess, canStop, cost * _alreadyVisitedCostFactor, turnCost);
        }

        return _originalCostFunction.Get(edgeEnumerator, forward, previousEdges);
    }
}
