using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;

namespace Itinero.Routing.Costs;

internal class ProfileCostFunction : ICostFunction
{
    private readonly Profile _profile;

    public ProfileCostFunction(Profile profile)
    {
        _profile = profile;
    }

    public (bool canAccess, bool canStop, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool forward = true,
        IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
    {
        previousEdges ??= ArraySegment<(EdgeId edgeId, byte? turn)>.Empty;

        var factor = _profile.FactorInEdgeDirection(edgeEnumerator);
        var length = edgeEnumerator.Length ??
                     (uint)(edgeEnumerator.EdgeLength() * 100);
        var directedFactor = forward ? factor.ForwardFactor : factor.BackwardFactor;
        var cost = directedFactor * length;
        var canAccess = directedFactor > 0;

        // check for turn costs.
        var totalTurnCost = 0.0;
        var (_, turn) = previousEdges.FirstOrDefault();
        if (turn == null) return (canAccess, factor.CanStop, cost, totalTurnCost);

        // there are turn costs.
        var turnCosts = edgeEnumerator.GetTurnCostToTail(turn.Value);
        foreach (var (_, attributes, turnCost, prefixEdges) in turnCosts)
        {
            // TODO: compare prefix edges with the previous edges.

            var turnCostFactor = _profile.TurnCostFactor(attributes);
            if (turnCostFactor.IsBinary && turnCost > 0)
            {
                totalTurnCost = double.MaxValue;
                break;
            }

            totalTurnCost += turnCostFactor.CostFactor * turnCost;
        }

        return (canAccess, factor.CanStop, cost, totalTurnCost);
    }
}
