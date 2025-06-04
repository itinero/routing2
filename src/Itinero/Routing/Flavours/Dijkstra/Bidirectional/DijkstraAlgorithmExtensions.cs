using System;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Snapping;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;

internal static class DijkstraAlgorithmExtensions
{
    public static void Push(this DijkstraAlgorithm dijkstraAlgorithm, ICostFunction costFunction, SnapPoint snapPoint,
        bool asOrigin)
    {
        var enumerator = dijkstraAlgorithm.RoutingNetwork.GetEdgeEnumerator();
        var costEnumerator = new CostEdgeEnumerator(enumerator, costFunction);

        if (asOrigin)
        {
            var (cost, _) = costEnumerator.MoveToAndGetCost(snapPoint.EdgeId, true, true, []);
            if (cost > 0) dijkstraAlgorithm.Push(snapPoint.EdgeId, true, cost * (1 - snapPoint.OffsetFactor()));

            (cost, _) = costEnumerator.MoveToAndGetCost(snapPoint.EdgeId, false, true, []);
            if (cost > 0) dijkstraAlgorithm.Push(snapPoint.EdgeId, false, cost * snapPoint.OffsetFactor());
        }
        else
        {
            var (cost, _) = costEnumerator.MoveToAndGetCost(snapPoint.EdgeId, true, false, []);
            if (cost > 0) dijkstraAlgorithm.Push(snapPoint.EdgeId, true, cost * (1 - snapPoint.OffsetFactor()));

            (cost, _) = costEnumerator.MoveToAndGetCost(snapPoint.EdgeId, false, false, []);
            if (cost > 0) dijkstraAlgorithm.Push(snapPoint.EdgeId, false, cost * snapPoint.OffsetFactor());
        }
    }

    public static Path GetPathToVisit(this DijkstraAlgorithm dijkstraAlgorithm, uint p)
    {
        var path = new Path(dijkstraAlgorithm.RoutingNetwork);
        var visit = dijkstraAlgorithm.GetVisit(p);

        while (true)
        {
            if (visit.previousPointer == uint.MaxValue)
            {
                path.Prepend(visit.edge, visit.forward);
                break;
            }

            path.Prepend(visit.edge, visit.forward);
            visit = dijkstraAlgorithm.GetVisit(visit.previousPointer);
        }

        return path;
    }
}
