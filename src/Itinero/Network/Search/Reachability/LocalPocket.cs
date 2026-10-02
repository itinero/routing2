using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;

namespace Itinero.Network.Search.Reachability;

/// <summary>
/// The local-access region around an endpoint: the edges a route may move through freely before it
/// is genuinely on the network.
/// </summary>
/// <remarks>
/// Reconstructs, lazily and around one endpoint, the class island detection precomputed per tile:
/// local-access edges plus the untagged segments enclosed by them.
/// </remarks>
internal static class LocalPocket
{
    /// <summary>
    /// Members are treated as local-access by the search; perimeter edges are the exits.
    /// </summary>
    /// <remarks>
    /// Abandoned means the region outgrew its budget, so it is not a pocket and the caller should
    /// behave as though none was found.
    /// </remarks>
    internal sealed class Result
    {
        public required HashSet<EdgeId> Members { get; init; }
        public required HashSet<EdgeId> Perimeter { get; init; }
        public required bool Abandoned { get; init; }
    }

    /// <summary>
    /// Composes the pocket around <paramref name="seeds"/>, in the half's own direction.
    /// </summary>
    /// <param name="seeds">
    /// The candidate edge, or the settled set of a half that stalled — already proved closed, so it
    /// costs nothing to start from.
    /// </param>
    /// <param name="asOrigin">True for a half leaving an origin, false for one arriving.</param>
    /// <param name="componentAllowance">
    /// How far an N component may be followed before it counts as main network. Exceeding it means
    /// "not absorbed", which is the safe direction: the strict rule then applies.
    /// </param>
    /// <param name="budget">Total members allowed before the region is too big to be a pocket.</param>
    public static Result Compose(
        RoutingNetwork network,
        ICostFunction costFunction,
        IEnumerable<EdgeId> seeds,
        bool asOrigin,
        int componentAllowance,
        int budget)
    {
        var members = new HashSet<EdgeId>(seeds);
        var perimeter = new HashSet<EdgeId>();

        // Edges in a component that outgrew its allowance. Main network is one component, so the
        // first seed to discover it answers for every later seed inside it.
        var knownMain = new HashSet<EdgeId>();

        var changed = true;
        while (changed)
        {
            if (members.Count > budget)
            {
                return new Result { Members = members, Perimeter = perimeter, Abandoned = true };
            }

            changed = false;

            // (a) Grow over local-access edges, and collect the N edges we end up against.
            // Materialised: the loop below adds to members, and Reachable enumerates it lazily.
            var nSeeds = new List<EdgeId>();
            foreach (var edge in Reachable(network, costFunction, members, asOrigin).ToList())
            {
                if (members.Contains(edge.edge) || perimeter.Contains(edge.edge)) continue;

                if (edge.localAccess)
                {
                    members.Add(edge.edge);
                    changed = true;
                }
                else
                {
                    nSeeds.Add(edge.edge);
                }
            }

            if (changed) continue;

            // (b) One pass over the N edges against the pocket, sharing knownMain so main network
            // is discovered once rather than per seed.
            foreach (var seed in nSeeds)
            {
                if (members.Contains(seed) || perimeter.Contains(seed)) continue;

                if (knownMain.Contains(seed))
                {
                    perimeter.Add(seed);
                    continue;
                }

                var (component, escaped) = NonLocalComponent(
                    network, costFunction, seed, asOrigin, componentAllowance, knownMain);

                if (escaped)
                {
                    knownMain.UnionWith(component);
                    perimeter.Add(seed);
                }
                else
                {
                    members.UnionWith(component);
                    changed = true;
                }
            }
        }

        return new Result { Members = members, Perimeter = perimeter, Abandoned = false };
    }

    /// The edges reachable in one step from any member, in the half's direction.
    private static IEnumerable<(EdgeId edge, bool localAccess)> Reachable(
        RoutingNetwork network, ICostFunction costFunction, HashSet<EdgeId> from, bool asOrigin)
    {
        var probe = network.GetEdgeEnumerator();
        var seen = new HashSet<EdgeId>();

        foreach (var member in from)
        {
            foreach (var vertex in EndsOf(probe, member, costFunction, asOrigin))
            {
                if (!probe.MoveTo(vertex)) continue;

                while (probe.MoveNext())
                {
                    if (probe.EdgeId == member) continue;
                    if (!seen.Add(probe.EdgeId)) continue;

                    var (canAccess, _, localAccess, cost, _) =
                        costFunction.Get(probe, tailToHead: asOrigin);
                    if (!canAccess || cost <= 0) continue;

                    yield return (probe.EdgeId, localAccess);
                }
            }
        }
    }

    /// <summary>
    /// The component of <paramref name="seed"/> over non-local edges only.
    /// </summary>
    /// <returns>
    /// The edges found, and whether it escaped — outgrew the allowance or ran into known main
    /// network, either of which means it is not enclosed.
    /// </returns>
    private static (HashSet<EdgeId> component, bool escaped) NonLocalComponent(
        RoutingNetwork network, ICostFunction costFunction, EdgeId seed, bool asOrigin,
        int allowance, HashSet<EdgeId> knownMain)
    {
        var probe = network.GetEdgeEnumerator();
        var component = new HashSet<EdgeId> { seed };
        var queue = new Queue<EdgeId>();
        queue.Enqueue(seed);

        var expansions = 0;
        while (queue.Count > 0)
        {
            if (++expansions > allowance) return (component, true);

            var edge = queue.Dequeue();
            if (knownMain.Contains(edge)) return (component, true);

            foreach (var vertex in EndsOf(probe, edge, costFunction, asOrigin))
            {
                if (!probe.MoveTo(vertex)) continue;

                while (probe.MoveNext())
                {
                    if (probe.EdgeId == edge) continue;

                    var (canAccess, _, localAccess, cost, _) =
                        costFunction.Get(probe, tailToHead: asOrigin);
                    if (!canAccess || cost <= 0) continue;

                    // Local edges bound the component rather than extending it.
                    if (localAccess) continue;
                    if (!component.Add(probe.EdgeId)) continue;

                    queue.Enqueue(probe.EdgeId);
                }
            }
        }

        return (component, false);
    }

    /// The vertices an edge can be left by, in the half's direction.
    private static List<VertexId> EndsOf(RoutingNetworkEdgeEnumerator probe, EdgeId edge,
        ICostFunction costFunction, bool asOrigin)
    {
        var ends = new List<VertexId>(2);
        foreach (var forward in new[] { true, false })
        {
            if (!probe.MoveTo(edge, forward)) continue;

            var (canAccess, _, _, cost, _) = costFunction.Get(probe, tailToHead: asOrigin);
            if (!canAccess || cost <= 0) continue;

            ends.Add(probe.Head);
        }

        return ends;
    }
}
