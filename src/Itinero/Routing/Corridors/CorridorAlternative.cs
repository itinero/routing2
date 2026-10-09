using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Routes.Paths;

namespace Itinero.Routing.Corridors;

/// <summary>
/// Picks alternative routes out of a corridor: cheapest first, each sharing no more than a given
/// share of its length with the routes already chosen, and each detouring for a reason.
/// </summary>
/// <remarks>
/// No searching: an edge's cheapest path is <c>CostFromOrigin[tail] + cost +
/// CostToDestination[head]</c>, and the budget defined the corridor, so it is never tested.
/// </remarks>
public static class CorridorAlternative
{
    /// <summary>
    /// Alternatives to the resolved route, cheapest first.
    /// </summary>
    /// <param name="maxOverlap">
    /// Share of a candidate's own length that may run along routes already chosen, 0 to 1. By
    /// length, and against the union of those chosen, so each differs from its predecessors too.
    /// </param>
    /// <param name="maxAlternatives">
    /// At most this many. <see cref="int.MaxValue"/> returns everything the corridor admits.
    /// </param>
    /// <param name="localDetourFactor">
    /// How much worse the stretch a candidate diverges over may be than the one it replaces.
    /// Without it a candidate can meet the budget overall and still spend it on one absurd loop.
    /// </param>
    /// <returns>Empty when the corridor admits none — an answer, not a failure.</returns>
    /// <remarks>
    /// Sound but not complete: each candidate is the cheapest path through one edge, so an
    /// alternative needing two distant edges at once is not reachable this way.
    /// </remarks>
    public static IReadOnlyList<Path> AlternativesFor(
        this RoutingNetwork network,
        ResolvedRoute route,
        RouteCorridor corridor,
        double maxOverlap,
        int maxAlternatives = 1,
        double localDetourFactor = 2.0)
    {
        if (maxOverlap is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(maxOverlap),
            "An overlap share is a fraction of the candidate's length.");
        if (maxAlternatives < 0) throw new ArgumentOutOfRangeException(nameof(maxAlternatives));
        if (localDetourFactor < 1) throw new ArgumentOutOfRangeException(nameof(localDetourFactor),
            "A detour cannot be required to beat the stretch it replaces.");

        var found = new List<Path>();
        if (maxAlternatives == 0 || corridor.Edges.Count == 0) return found;

        var outgoing = new List<int>[corridor.States.Count];
        var incoming = new List<int>[corridor.States.Count];
        for (var e = 0; e < corridor.Edges.Count; e++)
        {
            var edge = corridor.Edges[e];
            (outgoing[edge.Tail] ??= new List<int>()).Add(e);
            (incoming[edge.Head] ??= new List<int>()).Add(e);
        }

        // The optimum as corridor edges: the cheapest path through the corridor is the route it
        // was built around, which is what the detour test measures divergence against.
        var optimal = Descend(RouteCorridor.SourceNode, toDestination: true);
        if (optimal == null) return found;

        // Seeded with the original and grown as alternatives are accepted, so each one has to
        // differ from its predecessors too.
        var shared = new HashSet<EdgeId>();
        foreach (var e in optimal) shared.Add(corridor.Edges[e].Edge);

        var candidates = new List<(double Cost, int Edge)>();
        for (var e = 0; e < corridor.Edges.Count; e++)
        {
            var edge = corridor.Edges[e];
            if (shared.Contains(edge.Edge)) continue;

            var before = corridor.CostFromOrigin[edge.Tail];
            var after = corridor.CostToDestination[edge.Head];
            if (double.IsPositiveInfinity(before) || double.IsPositiveInfinity(after)) continue;

            candidates.Add((before + edge.Cost + after, e));
        }

        candidates.Sort((x, y) => x.Cost.CompareTo(y.Cost));

        var seen = new HashSet<string>();
        foreach (var (_, via) in candidates)
        {
            if (found.Count >= maxAlternatives) break;

            var edges = PathThrough(via);
            if (edges == null) continue;

            // Different via edges on the same detour rebuild the same path.
            if (!seen.Add(string.Join(",", edges))) continue;

            var length = 0d;
            var overlap = 0d;
            foreach (var e in edges)
            {
                var edge = corridor.Edges[e];
                length += edge.Length;
                if (shared.Contains(edge.Edge)) overlap += edge.Length;
            }

            if (length <= 0 || overlap / length > maxOverlap) continue;
            if (!DetourIsWorthIt(edges)) continue;

            var path = Build(edges);
            if (path == null) continue;

            foreach (var e in edges) shared.Add(corridor.Edges[e].Edge);
            found.Add(path);
        }

        return found;

        // Shortest prefix plus shortest suffix, so a candidate leaves the optimum once and
        // rejoins once: that single stretch is what this compares.
        bool DetourIsWorthIt(List<int> edges)
        {
            var head = 0;
            while (head < edges.Count && head < optimal.Count && edges[head] == optimal[head]) head++;

            var tail = 0;
            while (tail < edges.Count - head && tail < optimal.Count - head &&
                   edges[edges.Count - 1 - tail] == optimal[optimal.Count - 1 - tail]) tail++;

            var replaced = 0d;
            for (var i = head; i < optimal.Count - tail; i++) replaced += corridor.Edges[optimal[i]].Cost;

            var detour = 0d;
            for (var i = head; i < edges.Count - tail; i++) detour += corridor.Edges[edges[i]].Cost;

            // Nothing replaced means the candidate only adds, which no factor can excuse.
            if (replaced <= 0) return detour <= 0;

            return detour <= replaced * localDetourFactor;
        }

        List<int>? PathThrough(int via)
        {
            var before = Descend(corridor.Edges[via].Tail, toDestination: false);
            var after = Descend(corridor.Edges[via].Head, toDestination: true);
            if (before == null || after == null) return null;

            before.Add(via);
            before.AddRange(after);

            // Built from opposite ends, the halves can meet in the middle and the join then holds
            // a loop. Cutting it out would remove the via edge, so drop the candidate.
            var visited = new HashSet<int> { corridor.Edges[before[0]].Tail };
            foreach (var e in before)
            {
                if (!visited.Add(corridor.Edges[e].Head)) return null;
            }

            return before;
        }

        // Walk a field to its end. Every edge costs something, so each step strictly reduces
        // what remains and the walk cannot loop.
        List<int>? Descend(int from, bool toDestination)
        {
            var steps = new List<int>();
            var node = from;
            var stop = toDestination ? RouteCorridor.TargetNode : RouteCorridor.SourceNode;
            while (node != stop)
            {
                var step = Realising(toDestination ? outgoing[node] : incoming[node], node, toDestination);
                if (step < 0) return null;

                steps.Add(step);
                node = toDestination ? corridor.Edges[step].Head : corridor.Edges[step].Tail;
                if (steps.Count > corridor.Edges.Count) return null;
            }

            if (!toDestination) steps.Reverse();
            return steps;
        }

        int Realising(List<int>? adjacent, int node, bool toDestination)
        {
            if (adjacent == null) return -1;

            var target = toDestination ? corridor.CostToDestination[node] : corridor.CostFromOrigin[node];
            var best = -1;
            var bestGap = double.MaxValue;
            foreach (var e in adjacent)
            {
                var edge = corridor.Edges[e];
                var from = toDestination
                    ? corridor.CostToDestination[edge.Head]
                    : corridor.CostFromOrigin[edge.Tail];
                if (double.IsPositiveInfinity(from)) continue;

                // Exact arithmetic would give equality; costs are accumulated doubles, so take
                // the closest instead of trusting it.
                var gap = Math.Abs(from + edge.Cost - target);
                if (gap >= bestGap) continue;

                bestGap = gap;
                best = e;
            }

            return best;
        }

        // Offsets follow the rule the router's own path building uses: they trim the first and
        // last edge, which for any path between these two snap points are the snapped edges.
        Path? Build(List<int> edges)
        {
            var path = new Path(network);
            try
            {
                foreach (var e in edges)
                {
                    var edge = corridor.Edges[e];
                    path.Append(edge.Edge, edge.Forward);
                }
            }
            catch (Exception)
            {
                return null;
            }

            path.Offset1 = path.First.direction
                ? route.Source.Offset
                : (ushort)(ushort.MaxValue - route.Source.Offset);
            path.Offset2 = path.Last.direction
                ? route.Target.Offset
                : (ushort)(ushort.MaxValue - route.Target.Offset);
            return path;
        }
    }
}
