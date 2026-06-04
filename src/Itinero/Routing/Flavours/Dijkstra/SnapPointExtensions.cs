using System;
using Itinero.Network;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Snapping;

namespace Itinero.Routing.Flavours.Dijkstra;

/// <summary>
/// Extensions related to snap points.
/// </summary>
public static class SnapPointExtensions
{
    /// <summary>
    /// Returns a factor in the range [0, 1] representing the position on the edge.
    /// </summary>
    /// <param name="snapPoint">The snap point.</param>
    /// <returns>The factor.</returns>
    public static double OffsetFactor(this SnapPoint snapPoint)
    {
        return snapPoint.Offset / (double)ushort.MaxValue;
    }

    /// <summary>
    /// Calculates a single edge path using the given cost function if the two snap points are on the same edge.
    /// </summary>
    /// <param name="routingNetwork"></param>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    /// <param name="costFunction"></param>
    /// <param name="path"></param>
    /// <param name="cost"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static bool TrySingleHop(this RoutingNetwork routingNetwork,
        SnapPoint origin, SnapPoint destination, ICostFunction costFunction,
        out Path path, out double cost)
    {
        path = null;
        cost = double.MaxValue;
        if (origin.EdgeId != destination.EdgeId) return false;

        var enumerator = new CostEdgeEnumerator(routingNetwork.GetEdgeEnumerator(), costFunction);
        if (!enumerator.MoveTo(origin.EdgeId, true)) throw new Exception();
        var tailToHeadCost = enumerator.GetCost(true);
        var headToTailCost = enumerator.GetCost(false);
        if (headToTailCost.cost <= 0 && tailToHeadCost.cost <= 0) return false;

        if (origin.Offset == destination.Offset)
        {
            var tailToHead = tailToHeadCost.cost > 0;
            path = new Path(routingNetwork);
            path.Append(origin.EdgeId, tailToHead);
            path.Offset1 = origin.Offset;
            path.Offset2 = destination.Offset;
            cost = 0;
        }
        else if (origin.Offset < destination.Offset)
        {
            var tailToHead = tailToHeadCost.cost > 0;
            if (!tailToHead) return false;

            path = new Path(routingNetwork);
            path.Append(origin.EdgeId, true);
            path.Offset1 = origin.Offset;
            path.Offset2 = destination.Offset;

            cost = (destination.OffsetFactor() - origin.OffsetFactor()) * tailToHeadCost.cost;
        }
        else
        {
            var headToTail = headToTailCost.cost > 0;
            if (!headToTail) return false;

            path = new Path(routingNetwork);
            path.Append(origin.EdgeId, false);
            path.Offset1 = (ushort)(ushort.MaxValue - origin.Offset);
            path.Offset2 = (ushort)(ushort.MaxValue - destination.Offset);

            cost = (origin.OffsetFactor() - destination.OffsetFactor()) * headToTailCost.cost;
        }

        return true;
    }
}
