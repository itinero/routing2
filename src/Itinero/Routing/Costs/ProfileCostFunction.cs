using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;

using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Routing.Costs;

internal class ProfileCostFunction : ICostFunction
{
    private readonly Profile _profile;

    public ProfileCostFunction(Profile profile)
    {
        _profile = profile;
    }

    public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        PreviousEdgeEnumerable previousEdges = default)
    {

        var factor = _profile.FactorInEdgeDirection(edgeEnumerator);
        var length = edgeEnumerator.Length ??
                     (uint)(edgeEnumerator.EdgeLength() * 100);
        var directedFactor = tailToHead ? factor.ForwardFactor : factor.BackwardFactor;
        var cost = directedFactor * length;
        var canAccess = directedFactor > 0;
        var localAccess = factor.IsLocalAccess;

        // check for turn costs.
        var totalTurnCost = 0.0;
        var turn = previousEdges.First?.turn;
        if (turn == null) return (canAccess, factor.CanStop, localAccess, cost, totalTurnCost);

        // there are turn costs.
        var turnCosts = tailToHead
            ? edgeEnumerator.GetTurnCostToTail(turn.Value)
            : edgeEnumerator.GetTurnCostFromTail(turn.Value);
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

        return (canAccess, factor.CanStop, localAccess, cost, totalTurnCost);
    }
}
