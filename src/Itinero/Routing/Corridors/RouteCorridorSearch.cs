using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Reachability;
using Itinero.Routing.DataStructures;
using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Routing.Flavours.Dijkstra.Bidirectional;
using Itinero.Snapping;

namespace Itinero.Routing.Corridors;

/// <summary>
/// Extracts a cost ellipse around a route: every edge that lies on some origin-to-destination
/// path costing no more than a multiple of the optimum.
/// </summary>
/// <remarks>
/// Out of the route's own search: its two halves meet in the middle, so neither spans the ellipse
/// alone, and lifting the ceiling to the budget keeps what the search would examine anyway.
/// </remarks>
public static class RouteCorridorSearch
{
    /// <summary>
    /// Resolves the route and extracts the corridor around it, in one search.
    /// </summary>
    /// <param name="costFactor">
    /// The budget as a multiple of the optimum. 1.0 admits only optimal paths.
    /// </param>
    /// <returns>
    /// The route and the corridor around it. The route is handed back because the caller needs
    /// the endpoints and cost functions that were actually used, and this call consumed them.
    /// </returns>
    /// <remarks>
    /// At termination <c>budget &lt;= forwardFrontier + backwardFrontier</c>, so one half or the
    /// other relaxed every transition of a within-budget path: their union prices them all.
    /// </remarks>
    public static async Task<Result<(ResolvedRoute Route, RouteCorridor Corridor)>> RouteCorridorAsync(
        this RoutingNetwork network,
        RoutingSettings settings,
        (double longitude, double latitude) origin,
        (double longitude, double latitude) destination,
        ReachabilityBounds bounds,
        double searchBoxMeters,
        double costFactor,
        CancellationToken cancellationToken = default,
        double maxLocalDetour = double.MaxValue)
    {
        if (costFactor < 1.0) throw new ArgumentOutOfRangeException(nameof(costFactor),
            "A corridor cannot be tighter than the optimum it is measured against.");
        if (maxLocalDetour < 1.0) throw new ArgumentOutOfRangeException(nameof(maxLocalDetour),
            "A detour cannot be required to beat the stretch it replaces.");

        // Arrive at Turn over Prev, then traverse Next. Neither half reports in that convention,
        // so take the state triple only and price it below; the halves' overlap then dedupes free.
        var transitions = new HashSet<(EdgeId Prev, VertexId Turn, EdgeId Next)>();
        var relaxed = 0;

        void OnForward(EdgeId fromEdge, VertexId turnVertex, EdgeId toEdge)
        {
            relaxed++;
            transitions.Add((fromEdge, turnVertex, toEdge));
        }

        void OnBackward(EdgeId fromEdge, VertexId turnVertex, EdgeId toEdge)
        {
            // Against travel the roles swap: in path order the route arrives at turnVertex over
            // the edge being relaxed onto, and leaves over the one the half arrived by.
            relaxed++;
            transitions.Add((toEdge, turnVertex, fromEdge));
        }

        var resolved = await network.ResolveAsync(settings, origin, destination, bounds,
            searchBoxMeters, costFactor, OnForward, OnBackward, cancellationToken);
        if (resolved.IsError)
            return new Result<(ResolvedRoute, RouteCorridor)>(resolved.ErrorMessage);

        var route = resolved.Value;
        return new Result<(ResolvedRoute, RouteCorridor)>((route,
            Assemble(network, route, route.Cost * costFactor, transitions, relaxed, maxLocalDetour)));
    }

    /// <summary>
    /// Prices every transition in path order, then applies the corridor's own filter and trim.
    /// </summary>
    private static RouteCorridor Assemble(
        RoutingNetwork network,
        ResolvedRoute route,
        double budget,
        HashSet<(EdgeId Prev, VertexId Turn, EdgeId Next)> transitions,
        int relaxed,
        double maxLocalDetour)
    {
        var probe = network.GetEdgeEnumerator();
        var orders = network.GetEdgeEnumerator();
        var costFunction = route.ForwardCostFunction;
        var lengths = new Dictionary<EdgeId, double>();

        var states = new List<CorridorState?> { null, null };
        var nodes = new Dictionary<(EdgeId, VertexId), int>();
        var byVertex = new Dictionary<VertexId, List<int>>();
        var tails = new List<int>(transitions.Count);
        var heads = new List<int>(transitions.Count);
        var costs = new List<double>(transitions.Count);

        foreach (var (prev, turn, next) in transitions)
        {
            // Position on Next with Turn as its tail: that is the direction the path travels it.
            if (!probe.MoveTo(next, true)) continue;
            if (probe.Tail != turn && !(probe.MoveTo(next, false) && probe.Tail == turn)) continue;

            var prevOrder = OrderAt(orders, prev, turn);
            var previous = prevOrder.HasValue
                ? PreviousEdgeEnumerable.ForEdge(prev, prevOrder)
                : default;
            var (canAccess, _, _, cost, turnCost) =
                costFunction.Get(probe, tailToHead: true, previous);
            if (!canAccess || cost is >= double.MaxValue or <= 0) continue;
            if (turnCost is >= double.MaxValue or < 0) continue;

            // Which way Prev was travelled follows from where it ends, and it ends at the turn.
            var prevForward = orders.MoveTo(prev, true) && orders.Head == turn;
            Add(Node(new CorridorState(prev, prevForward, turn)),
                Node(new CorridorState(next, probe.Forward, probe.Head)),
                cost + turnCost);
        }

        var sourcePartials = AddSourcePartials();
        var destinationPartials = AddDestinationPartials();

        var fromOrigin = Distances(tails, heads, costs, states.Count, budget,
            RouteCorridor.SourceNode, forwards: true);
        var toDestination = Distances(tails, heads, costs, states.Count, budget,
            RouteCorridor.TargetNode, forwards: false);

        // Edges reachable only by a detour absurd for what it replaces; the trip-wide budget
        // cannot see those. Both fields are recomputed after, since they are published as exact.
        bool[]? alive = null;
        if (maxLocalDetour < double.MaxValue)
        {
            alive = LocallyReasonable(tails, heads, costs, states.Count, fromOrigin, toDestination,
                maxLocalDetour);
            fromOrigin = Distances(tails, heads, costs, states.Count, budget,
                RouteCorridor.SourceNode, forwards: true, alive);
            toDestination = Distances(tails, heads, costs, states.Count, budget,
                RouteCorridor.TargetNode, forwards: false, alive);
        }

        var surviving = new List<int>();
        var originEdges = 0;
        var interiorEdges = 0;
        var destinationEdges = 0;
        for (var i = 0; i < tails.Count; i++)
        {
            if (alive != null && !alive[i]) continue;

            var before = fromOrigin[tails[i]];
            var after = toDestination[heads[i]];
            if (double.IsPositiveInfinity(before) || double.IsPositiveInfinity(after)) continue;
            if (before + costs[i] + after > budget) continue;

            if (tails[i] == RouteCorridor.SourceNode) originEdges++;
            else if (heads[i] == RouteCorridor.TargetNode) destinationEdges++;
            else interiorEdges++;
            surviving.Add(i);
        }

        var connected = Trim(tails, heads, surviving, states.Count);
        var edges = new List<CorridorEdge>(connected.Count);
        foreach (var i in connected) edges.Add(Materialise(i));
        var (compactStates, compactEdges, compactFromOrigin, compactToDestination) =
            Compact(states, edges, fromOrigin, toDestination);

        var optimalLength = 0d;
        foreach (var (edge, _, offset1, offset2) in route.Path)
        {
            optimalLength += Length(edge) * ((offset2 - offset1) / (double)ushort.MaxValue);
        }

        return new RouteCorridor(compactStates, compactEdges, route.Cost, optimalLength,
            route.Settled, relaxed, surviving.Count - connected.Count,
            originEdges, interiorEdges, destinationEdges, compactFromOrigin, compactToDestination);

        // Rebuilt rather than captured from the seeds: a candidate retry re-seeds, and those
        // seeds belong to a snap point this route no longer uses.
        HashSet<int> AddSourcePartials()
        {
            var partials = new HashSet<int>();
            foreach (var forward in new[] { true, false })
            {
                if (!probe.MoveTo(route.Source.EdgeId, forward)) continue;

                var (canAccess, _, _, cost, _) = costFunction.Get(probe, tailToHead: true);
                if (!canAccess || cost <= 0) continue;

                partials.Add(tails.Count);
                Add(RouteCorridor.SourceNode,
                    Node(new CorridorState(route.Source.EdgeId, probe.Forward, probe.Head)),
                    cost * (forward ? 1 - route.Source.OffsetFactor() : route.Source.OffsetFactor()));
            }

            return partials;
        }

        // Into the destination, from every state that can turn onto the target edge. The turn is
        // charged here because it exists only once the two sides are composed.
        Dictionary<int, bool> AddDestinationPartials()
        {
            var partials = new Dictionary<int, bool>();
            var backwardCostFunction = route.BackwardCostFunction;
            foreach (var forward in new[] { true, false })
            {
                if (!probe.MoveTo(route.Target.EdgeId, forward)) continue;

                var (canAccess, _, _, cost, _) = backwardCostFunction.Get(probe, tailToHead: false);
                if (!canAccess || cost <= 0) continue;

                var at = probe.Head;
                if (!byVertex.TryGetValue(at, out var arrivals)) continue;

                var partialCost =
                    cost * (forward ? 1 - route.Target.OffsetFactor() : route.Target.OffsetFactor());
                foreach (var node in arrivals)
                {
                    var arrival = states[node]!.Value;
                    if (arrival.Edge == route.Target.EdgeId) continue; // U-turn onto the target.

                    var turnCost = SearchHalf.TurnCostBetween(orders, backwardCostFunction,
                        arrival.Edge, OrderAt(orders, arrival.Edge, at), route.Target.EdgeId, forward);
                    if (turnCost is >= double.MaxValue or < 0) continue;

                    partials[tails.Count] = forward;
                    Add(node, RouteCorridor.TargetNode, partialCost + turnCost);
                }
            }

            return partials;
        }

        CorridorEdge Materialise(int i)
        {
            if (destinationPartials.TryGetValue(i, out var intoForward))
            {
                // intoForward is the orientation the backward half probed the target edge with,
                // and it reached the turn at that edge's head — so travel runs the other way.
                return new CorridorEdge(tails[i], heads[i], costs[i],
                    Partial(route.Target.EdgeId, intoForward, route.Target),
                    route.Target.EdgeId, !intoForward,
                    intoForward ? CorridorHalf.TargetBackwardFromHead : CorridorHalf.TargetForwardFromTail);
            }

            var state = states[heads[i]]!.Value;
            if (sourcePartials.Contains(i))
            {
                return new CorridorEdge(tails[i], heads[i], costs[i],
                    Partial(state.Edge, state.Forward, route.Source), state.Edge, state.Forward,
                    state.Forward ? CorridorHalf.SourceForwardToHead : CorridorHalf.SourceBackwardToTail);
            }

            return new CorridorEdge(tails[i], heads[i], costs[i], Length(state.Edge),
                state.Edge, state.Forward, CorridorHalf.None);
        }

        void Add(int tail, int head, double cost)
        {
            tails.Add(tail);
            heads.Add(head);
            costs.Add(cost);
        }

        int Node(CorridorState state)
        {
            var key = (state.Edge, state.Vertex);
            if (nodes.TryGetValue(key, out var index)) return index;

            index = states.Count;
            states.Add(state);
            nodes[key] = index;
            if (!byVertex.TryGetValue(state.Vertex, out var at))
            {
                at = new List<int>(2);
                byVertex[state.Vertex] = at;
            }
            at.Add(index);
            return index;
        }

        double Length(EdgeId edge)
        {
            if (lengths.TryGetValue(edge, out var length)) return length;

            length = probe.MoveTo(edge, true) ? probe.EdgeLength() : 0d;
            lengths[edge] = length;
            return length;
        }

        // The same fraction of the edge the partial's cost was scaled by.
        double Partial(EdgeId edge, bool towardHead, SnapPoint snap) =>
            Length(edge) * (towardHead ? 1 - snap.OffsetFactor() : snap.OffsetFactor());
    }

    /// The order of <paramref name="edge"/> at <paramref name="vertex"/>, whichever end that is.
    private static byte? OrderAt(
        RoutingNetworkEdgeEnumerator enumerator, EdgeId edge, VertexId vertex)
    {
        if (!enumerator.MoveTo(edge, true)) return null;

        return enumerator.Head == vertex ? enumerator.HeadOrder
            : enumerator.Tail == vertex ? enumerator.TailOrder
            : null;
    }

    /// <summary>
    /// Drops every edge that does not lie on some origin-to-destination path inside the corridor.
    /// </summary>
    /// <remarks>
    /// The cost filter proves a continuation exists in the network, not in the corridor, so a
    /// transition can price inside the budget with nothing to continue into. Not visibly wrong.
    /// </remarks>
    private static List<int> Trim(
        List<int> tails, List<int> heads, List<int> candidates, int nodeCount)
    {
        var outgoing = new List<int>[nodeCount];
        var incoming = new List<int>[nodeCount];
        foreach (var e in candidates)
        {
            (outgoing[tails[e]] ??= new List<int>()).Add(e);
            (incoming[heads[e]] ??= new List<int>()).Add(e);
        }

        var fromOrigin = Reach(RouteCorridor.SourceNode, outgoing, forwards: true);
        var toDestination = Reach(RouteCorridor.TargetNode, incoming, forwards: false);

        var kept = new List<int>(candidates.Count);
        foreach (var e in candidates)
        {
            if (fromOrigin[tails[e]] && toDestination[heads[e]]) kept.Add(e);
        }

        return kept;

        bool[] Reach(int start, List<int>[] adjacency, bool forwards)
        {
            var seen = new bool[nodeCount];
            var stack = new Stack<int>();
            seen[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                foreach (var e in adjacency[stack.Pop()] ?? [])
                {
                    var next = forwards ? heads[e] : tails[e];
                    if (seen[next]) continue;

                    seen[next] = true;
                    stack.Push(next);
                }
            }

            return seen;
        }
    }

    /// <summary>
    /// Shortest paths over the traced graph, out of a node or into it.
    /// </summary>
    /// <remarks>
    /// Over the traced graph, not either half's settled costs: each may only enter the pocket it
    /// started in, but their union holds every within-budget path in full.
    /// </remarks>
    private static double[] Distances(
        List<int> tails, List<int> heads, List<double> costs, int nodeCount, double budget,
        int start, bool forwards, bool[]? alive = null)
    {
        var adjacency = new List<int>[nodeCount];
        for (var e = 0; e < tails.Count; e++)
        {
            if (alive != null && !alive[e]) continue;

            (adjacency[forwards ? tails[e] : heads[e]] ??= new List<int>()).Add(e);
        }

        var distances = new double[nodeCount];
        Array.Fill(distances, double.PositiveInfinity);
        distances[start] = 0;

        var queue = new BinaryHeap<int>();
        queue.Push(start, 0);
        var settled = new bool[nodeCount];

        while (queue.Count > 0)
        {
            var node = queue.Pop(out var cost);
            if (settled[node]) continue;

            settled[node] = true;
            if (cost > budget) break;

            foreach (var e in adjacency[node] ?? [])
            {
                var next = forwards ? heads[e] : tails[e];
                var candidate = cost + costs[e];
                if (candidate >= distances[next] || candidate > budget) continue;

                distances[next] = candidate;
                queue.Push(next, candidate);
            }
        }

        return distances;
    }


    /// <summary>
    /// Which edges are reachable without a local detour worse than <paramref name="maxDetour"/>
    /// times the stretch it replaces. Per edge, so a flow model with no single path can use it.
    /// </summary>
    /// <remarks>
    /// O(1) an edge: its cheapest path leaves the optimum at A and rejoins at B, both on the
    /// optimum, so the replaced stretch costs <c>d_D(A) - d_D(B)</c> with no path walked.
    /// </remarks>
    private static bool[] LocallyReasonable(
        List<int> tails, List<int> heads, List<double> costs, int nodeCount,
        double[] fromOrigin, double[] toDestination, double maxDetour)
    {
        var alive = new bool[tails.Count];
        Array.Fill(alive, true);

        // The predecessor and successor each field's descent would pick, per node.
        var toward = new int[nodeCount];
        var onward = new int[nodeCount];
        Array.Fill(toward, -1);
        Array.Fill(onward, -1);
        for (var e = 0; e < tails.Count; e++)
        {
            if (Realises(fromOrigin, heads[e], tails[e], costs[e])) toward[heads[e]] = e;
            if (Realises(toDestination, tails[e], heads[e], costs[e])) onward[tails[e]] = e;
        }

        // The optimum: descend to the destination from the origin. Its nodes are the ones a
        // detour can be measured against.
        var onOptimum = new bool[nodeCount];
        var at = RouteCorridor.SourceNode;
        for (var guard = 0; at != RouteCorridor.TargetNode && guard <= tails.Count; guard++)
        {
            onOptimum[at] = true;
            var step = onward[at];
            if (step < 0) return alive; // no optimum to compare against; keep everything

            at = heads[step];
        }

        onOptimum[RouteCorridor.TargetNode] = true;

        var ancestor = Nearest(toward, tails, heads, onOptimum, towardOrigin: true);
        var descendant = Nearest(onward, tails, heads, onOptimum, towardOrigin: false);

        for (var e = 0; e < tails.Count; e++)
        {
            var a = ancestor[tails[e]];
            var b = descendant[heads[e]];
            if (a < 0 || b < 0) continue;

            var replaced = toDestination[a] - toDestination[b];
            var detour = (fromOrigin[tails[e]] - fromOrigin[a]) + costs[e]
                         + (toDestination[heads[e]] - toDestination[b]);
            if (detour <= 0) continue;

            // Leaving and rejoining at the same point replaces nothing, which no factor excuses.
            alive[e] = replaced > 0 && detour <= replaced * maxDetour;
        }

        return alive;

        static bool Realises(double[] field, int from, int to, double cost) =>
            !double.IsPositiveInfinity(field[from]) && !double.IsPositiveInfinity(field[to]) &&
            Math.Abs(field[to] + cost - field[from]) < 1e-6;

        // Walk each node to the first optimum node its descent reaches, memoised, so the whole
        // map costs one pass rather than one walk per edge.
        int[] Nearest(int[] step, List<int> t, List<int> h, bool[] optimum, bool towardOrigin)
        {
            var nearest = new int[nodeCount];
            Array.Fill(nearest, -2);
            for (var n = 0; n < nodeCount; n++) Resolve(n);

            return nearest;

            int Resolve(int node)
            {
                if (nearest[node] != -2) return nearest[node];
                if (optimum[node]) return nearest[node] = node;

                nearest[node] = -1; // breaks a cycle rather than recursing into it
                var e = step[node];
                if (e < 0) return nearest[node] = -1;

                return nearest[node] = Resolve(towardOrigin ? t[e] : h[e]);
            }
        }
    }

    /// <summary>
    /// Renumbers the nodes the surviving edges actually use, dropping the rest.
    /// </summary>
    /// <remarks>
    /// Numbering happens while the trace is assembled, so most entries do not survive the filter;
    /// the gaps would otherwise make every caller walk nodes no edge refers to.
    /// </remarks>
    private static (List<CorridorState?> states, List<CorridorEdge> edges,
        List<double> fromOrigin, List<double> toDestination)
        Compact(List<CorridorState?> states, List<CorridorEdge> edges,
            double[] fromOrigin, double[] toDestination)
    {
        var renumbered = new int[states.Count];
        Array.Fill(renumbered, -1);
        renumbered[RouteCorridor.SourceNode] = RouteCorridor.SourceNode;
        renumbered[RouteCorridor.TargetNode] = RouteCorridor.TargetNode;

        var compactStates = new List<CorridorState?> { null, null };
        var compactFromOrigin = new List<double>
            { fromOrigin[RouteCorridor.SourceNode], fromOrigin[RouteCorridor.TargetNode] };
        var compactToDestination = new List<double>
            { toDestination[RouteCorridor.SourceNode], toDestination[RouteCorridor.TargetNode] };
        var compactEdges = new List<CorridorEdge>(edges.Count);
        foreach (var edge in edges)
        {
            compactEdges.Add(edge with { Tail = Renumber(edge.Tail), Head = Renumber(edge.Head) });
        }

        return (compactStates, compactEdges, compactFromOrigin, compactToDestination);

        int Renumber(int node)
        {
            if (renumbered[node] >= 0) return renumbered[node];

            renumbered[node] = compactStates.Count;
            compactStates.Add(states[node]);
            compactFromOrigin.Add(fromOrigin[node]);
            compactToDestination.Add(toDestination[node]);
            return renumbered[node];
        }
    }
}
