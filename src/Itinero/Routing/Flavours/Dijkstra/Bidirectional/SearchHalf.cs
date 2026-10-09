using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Reachability;
using Itinero.Routing.Costs;
using Itinero.Routing.DataStructures;
using Itinero.Snapping;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;

/// <summary>
/// One side of a bidirectional search: everything reached so far from one endpoint, and
/// everywhere it could go next.
/// </summary>
/// <remarks>
/// A type of its own because a half outlives the call that created it: the half the snapper
/// expands to check reachability IS the half the search continues with.
/// </remarks>
internal sealed class SearchHalf
{
    /// <summary>
    /// Whether this half grows forward from an origin or backward from a destination; it decides
    /// which way edge costs are read, so it belongs to the half rather than to each step.
    /// </summary>
    public bool IsForwardHalf { get; }


    /// <summary>
    /// The edge at the other end of the route. Set so this half can notice when it gets there.
    /// </summary>
    public EdgeId? OtherEndpointEdge { get; set; }

    /// <summary>
    /// Whether this half has settled a state on <see cref="OtherEndpointEdge"/>.
    /// </summary>
    /// <remarks>
    /// With an empty heap this separates "nowhere left to go" from "already there": no path
    /// exists, so the other half need not walk to the tile budget to find that out.
    /// </remarks>
    public bool ReachedOtherEndpoint { get; private set; }

    public SearchHalf(bool isForwardHalf)
    {
        this.IsForwardHalf = isForwardHalf;
    }

    /// <summary>
    /// Whether this half has traversed a local-access edge. A closed component out in ordinary
    /// road network is an island and there is nothing more to learn about it; among
    /// access=destination edges it is worth a second opinion before rejecting a snap.
    /// </summary>
    public bool SawLocalAccess { get; private set; }

    /// <summary>
    /// Local-access states pushed but not yet settled. Zero means the frontier has left the
    /// enclave rather than merely touched its edge.
    /// </summary>
    /// <remarks>
    /// Counted rather than scanned: asked after every step, so walking the heap would make
    /// escaping a pocket quadratic in its size.
    /// </remarks>
    public int LocalOnHeap { get; private set; }

    public readonly PathTree Tree = new();

    // Carries g because the heap is ordered by g + p; only the stopping rule uses the key.
    public readonly BinaryHeap<(uint pointer, EdgeId edge, VertexId vertex, double g)> Heap = new();
    public readonly HashSet<(EdgeId edge, VertexId vertex)> Settled = new();

    // Per-vertex list of settled arrivals. Each entry records the incoming edge plus the head
    // order at this vertex for that edge — both are needed to query turn costs against this
    // label when the other half asks "can I meet you here?".
    public readonly Dictionary<VertexId, List<SettledEntry>> SettledByVertex = new();

    public void Clear()
    {
        Tree.Clear();
        Heap.Clear();
        Settled.Clear();
        SettledByVertex.Clear();
        this.SawLocalAccess = false;
        this.LocalOnHeap = 0;
        this.ReachedOtherEndpoint = false;
    }

    /// Pops one state, keeping the local-frontier count in step.
    private (uint pointer, EdgeId edge, VertexId vertex, double g) PopCounted(out double key)
    {
        var entry = Heap.Pop(out key);
        var (_, _, _, _, localAccess, _, _) = Tree.GetVisitWithState(entry.pointer);
        if (localAccess) this.LocalOnHeap--;
        return entry;
    }

    /// Pushes one state, keeping the local-frontier count in step.
    private void PushCounted((uint pointer, EdgeId edge, VertexId vertex, double g) entry,
        double key, bool localAccess)
    {
        if (localAccess) this.LocalOnHeap++;
        Heap.Push(entry, key);
    }

    /// <summary>
    /// Seeds this half from a snapped point: the states you can be in having just started on
    /// that edge, at that offset.
    /// </summary>
    public void PushTerminal(
        RoutingNetworkEdgeEnumerator enumerator,
        SnapPoint snap,
        ICostFunction costFunction,
        HeuristicFunc? potential)
    {
        // Forward half computes cost in the edge's natural direction, backward half against it,
        // so the backward half reaches "back toward the origin" with the right weights.
        foreach (var forward in new[] { true, false })
        {
            if (!enumerator.MoveTo(snap.EdgeId, forward)) continue;
            var (canAccess, _, localAccess, cost, _) = costFunction.Get(enumerator, tailToHead: this.IsForwardHalf);
            if (!canAccess || cost <= 0) continue;
            var offsetCost = forward
                ? cost * (1 - snap.OffsetFactor())
                : cost * snap.OffsetFactor();
            this.SawLocalAccess |= localAccess;
            var p = Tree.AddVisit(enumerator, leftMain: false, localAccess: localAccess, uint.MaxValue);
            this.PushCounted((p, enumerator.EdgeId, enumerator.Head, offsetCost),
                offsetCost + Potential(potential, enumerator), localAccess);

        }
    }

    /// <summary>
    /// Rebuilds the frontier's ordering against a goal-direction potential.
    /// </summary>
    /// <remarks>
    /// An expansion with no goal keyed its frontier with p = 0; mixing that with potential-keyed
    /// states settles them out of order, which in a Dijkstra is a wrong answer, not a slow one.
    /// </remarks>
    public void ReKey(RoutingNetwork network, HeuristicFunc? potential)
    {
        if (potential == null || Heap.Count == 0) return;

        var entries = new List<(uint pointer, EdgeId edge, VertexId vertex, double g)>(Heap.Count);
        while (Heap.Count > 0) entries.Add(Heap.Pop(out _));

        var enumerator = network.GetEdgeEnumerator();
        foreach (var entry in entries)
        {
            // Position on the arrival so the potential is taken at the state's own location,
            // matching how the key was formed when the search pushes normally.
            var p = 0d;
            if (enumerator.MoveTo(entry.edge, true))
            {
                p = enumerator.Head == entry.vertex
                    ? Potential(potential, enumerator)
                    : enumerator.MoveTo(entry.edge, false) ? Potential(potential, enumerator) : 0d;
            }

            Heap.Push(entry, entry.g + p);
        }
    }

    /// <summary>
    /// Expands out of a local-access enclave, stopping once it is properly clear of it.
    /// </summary>
    /// <param name="threshold">
    /// How much must be explored before clearing the enclave counts; without it, stepping onto
    /// the first ordinary road would stop the search having proven nothing.
    /// </param>
    /// <param name="ceiling">
    /// Work limit. Reaching it means the area is too big to treat as an enclave at all.
    /// </param>
    /// <remarks>
    /// Both halves of the stopping condition are load-bearing: the threshold alone can stop deep
    /// inside the enclave, the non-local frontier alone one edge out of a driveway.
    /// </remarks>
    public async Task<ReachabilityVerdict> EscapeLocalAsync(
        RoutingNetwork network,
        ICostFunction costFunction,
        int threshold,
        int ceiling,
        CancellationToken cancellationToken = default)
    {
        var expanded = 0;
        while (Heap.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await this.StepAsync(network, costFunction, isMainN: null, localAccessRule: true,
                bestCost: double.MaxValue, settledCb: LoadTileFor, potential: null,
                onReached: null, cancellationToken);

            expanded++;

            // Too big to be an enclave. Not a statement that the search got out of anything —
            // a statement that there is nothing here to get out of.
            if (expanded >= ceiling) return ReachabilityVerdict.BeyondBound;

            if (expanded >= threshold && this.LocalOnHeap == 0) return ReachabilityVerdict.BeyondBound;
        }

        // Ran out of network. Everything reachable from the enclave has been seen and it does
        // not lead anywhere, whether or not it ever got onto ordinary roads.
        return ReachabilityVerdict.Island;

        async Task<bool> LoadTileFor(VertexId v)
        {
            if (!network.UsageNotifier.IsVertexDataReady(network, v))
            {
                await network.UsageNotifier.NotifyVertex(network, v, cancellationToken);
            }

            return false;
        }
    }

    /// <summary>
    /// Expands until this half either runs out of network or has gone far enough to be believed —
    /// the reachability check a snap needs, done by the search that will continue from here.
    /// </summary>
    /// <param name="ceiling">
    /// States to expand before concluding the network is real. Running out first means the
    /// component is closed, which is an island whatever its size.
    /// </param>
    /// <remarks>
    /// No access rule here, deliberately: the question is what can be reached, not what route is
    /// legal, so a non-local road buried in an estate is crossed rather than refused.
    /// </remarks>
    public async Task<ReachabilityVerdict> ExpandAsync(
        RoutingNetwork network,
        ICostFunction costFunction,
        int ceiling,
        CancellationToken cancellationToken = default)
    {
        var expanded = 0;
        while (Heap.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The same access rule the search itself applies, and it has to be: these states
            // become the search's frontier, so anything allowed here the finished route may do.
            await this.StepAsync(network, costFunction, isMainN: null, localAccessRule: true,
                bestCost: double.MaxValue, settledCb: LoadTileFor, potential: null,
                onReached: null, cancellationToken);

            expanded++;
            if (expanded >= ceiling) return ReachabilityVerdict.BeyondBound;
        }

        // Ran out of network: everything reachable has been seen and none of it leaves.
        return ReachabilityVerdict.Island;

        // Pulls in a vertex's tile before expanding from it. Without this the walk stops at the
        // edge of whatever happens to be loaded and reports an island wherever the data ran out.
        async Task<bool> LoadTileFor(VertexId v)
        {
            if (!network.UsageNotifier.IsVertexDataReady(network, v))
            {
                await network.UsageNotifier.NotifyVertex(network, v, cancellationToken);
            }

            return false;
        }
    }

    private RoutingNetwork? _probeNetwork;

    private RoutingNetworkEdgeEnumerator? _probe;

    /// <summary>
    /// The edge enumerator this half expands with, created once and reused.
    /// </summary>
    /// <remarks>
    /// A fresh one per step was three allocations per settled state and the top two entries in
    /// the allocation profile. Safe because a half is never stepped concurrently with itself.
    /// </remarks>
    private RoutingNetworkEdgeEnumerator Probe(RoutingNetwork network)
    {
        if (_probe != null && ReferenceEquals(_probeNetwork, network)) return _probe;

        _probe = network.GetEdgeEnumerator();
        _probeNetwork = network;

        return _probe;
    }

    /// <summary>
    /// Settles the cheapest state on the frontier and relaxes its neighbours.
    /// </summary>
    /// <param name="onReached">
    /// Invoked for every state settled or queued; null when a half expands alone for a snapping
    /// check, because there is no other half yet.
    /// </param>
    /// <param name="bestCost">
    /// Cost of the best complete path so far, used to prune. No bound yet means MaxValue.
    /// </param>
    /// <returns>The key of the state settled, or of the last one popped if already settled.</returns>
    public async ValueTask<double?> StepAsync(
        RoutingNetwork network,
        ICostFunction costFunction,
        IsMainNFunc? isMainN,
        bool localAccessRule,
        double bestCost,
        Func<VertexId, Task<bool>>? settledCb,
        HeuristicFunc? potential,
        ReachedCallback? onReached,
        CancellationToken cancellationToken,
        RelaxedCallback? onRelaxed = null)
    {
        // Dequeue, skipping already-settled labels. `key` is g + p, `cost` is g.
        // PopCounted keeps LocalOnHeap in step: every pushed state is popped exactly once,
        // including the ones skipped here as already settled, so the count balances.
        var entry = this.PopCounted(out var key);
        while (Settled.Contains((entry.edge, entry.vertex)))
        {
            if (Heap.Count == 0) return key;
            entry = this.PopCounted(out key);
        }
        if (!Settled.Add((entry.edge, entry.vertex))) return key;

        var cost = entry.g;

        var (vertex, edge, forward, _, localAccess, headOrder, _) = Tree.GetVisitWithState(entry.pointer);
        this.SawLocalAccess |= localAccess;

        if (this.OtherEndpointEdge.HasValue && edge == this.OtherEndpointEdge.Value)
        {
            this.ReachedOtherEndpoint = true;
        }

        // Record in per-vertex settled multimap for meeting checks initiated by the other half.
        if (!SettledByVertex.TryGetValue(vertex, out var list))
        {
            list = new List<SettledEntry>(2);
            SettledByVertex[vertex] = list;
        }
        list.Add(new SettledEntry(edge, forward, headOrder, entry.pointer, cost));

        // Settled callback semantics match the unidirectional edge-based: returning true
        // means "stop expanding from this vertex" (e.g. outside the max-distance box).
        if (settledCb != null && await settledCb(vertex)) return key;
        if (cancellationToken.IsCancellationRequested) return key;

        onReached?.Invoke(edge, forward, headOrder, entry.pointer, cost, vertex);

        // Expand neighbours.
        var probe = this.Probe(network);
        if (!probe.MoveTo(vertex)) return key;
        while (probe.MoveNext())
        {
            var neighbourEdge = probe.EdgeId;
            if (neighbourEdge == edge) continue; // no U-turn

            // Cost in this half's direction. Forward uses tailToHead=true, backward uses false.
            // PreviousEdgeEnumerable provides turn-cost context from the path so far.
            var prev = new PreviousEdgeEnumerable(Tree, entry.pointer);
            var (canAccess, _, neighbourLocalAccess, neighbourCost, turnCost) =
                costFunction.Get(probe, tailToHead: this.IsForwardHalf, prev);
            if (!canAccess || neighbourCost is >= double.MaxValue or <= 0) continue;
            if (turnCost is >= double.MaxValue or < 0) continue;
            this.SawLocalAccess |= neighbourLocalAccess;

            // A local-access edge may only be reached from another, so each half stays inside its
            // own pocket and cannot use access=destination streets as a shortcut between roads.
            if (localAccessRule)
            {
                // L to L, L to N, N to N. Never N to L: a half leaves its pocket once and never
                // enters another, so the composed path is pocket, network, pocket.
                if (neighbourLocalAccess && !localAccess) continue;
            }
            // Per-half access-aware rule: reject prev_main && !curr_main in this half's direction.
            else if (isMainN != null)
            {
                var prevMain = IsMain(edge, localAccess, isMainN);
                var currMain = IsMain(neighbourEdge, neighbourLocalAccess, isMainN);
                if (prevMain && !currMain) continue;
            }

            var totalCost = cost + neighbourCost + turnCost;
            if (totalCost >= bestCost) continue;

            var neighbourPointer = Tree.AddVisit(probe, leftMain: false, localAccess: neighbourLocalAccess, entry.pointer);

            // Meeting check against the other half's settled set BEFORE pushing — mirrors the
            // existing vertex-based pattern's OnQueued hook.
            onReached?.Invoke(neighbourEdge, probe.Forward, probe.HeadOrder, neighbourPointer,
                totalCost, probe.Head);
            onRelaxed?.Invoke(edge, vertex, neighbourEdge);

            this.PushCounted((neighbourPointer, neighbourEdge, probe.Head, totalCost),
                totalCost + Potential(potential, probe), neighbourLocalAccess);
        }

        return key;
    }

    /// The half's potential at the edge's head; 0 without one. HeadLocation is memoised.
    private static double Potential(HeuristicFunc? potential, RoutingNetworkEdgeEnumerator enumerator)
    {
        if (potential == null) return 0d;

        var (longitude, latitude, _) = enumerator.HeadLocation;
        return potential(longitude, latitude);
    }

    /// <summary>
    /// The turn where one half's arrival continues into the other's. Neither half charged it: the
    /// turn only exists once the two are composed, which also inverts pathOutgoing's direction.
    /// </summary>
    /// <returns><see cref="double.MaxValue"/> when the turn is forbidden or the edge is gone.</returns>
    internal static double TurnCostBetween(
        RoutingNetworkEdgeEnumerator enumerator,
        ICostFunction costFunction,
        EdgeId pathIncoming,
        byte? pathIncomingHeadOrder,
        EdgeId pathOutgoing,
        bool pathOutgoingForwardFromOther)
    {
        // Position at the outgoing edge in path-outgoing direction (the shared vertex is tail).
        if (!enumerator.MoveTo(pathOutgoing, !pathOutgoingForwardFromOther)) return double.MaxValue;

        var previous = pathIncomingHeadOrder.HasValue
            ? PreviousEdgeEnumerable.ForEdge(pathIncoming, pathIncomingHeadOrder)
            : default;
        var (_, _, _, _, turnCost) = costFunction.Get(enumerator, tailToHead: true, previous);
        return turnCost;
    }

    private static bool IsMain(EdgeId edgeId, bool localAccess, IsMainNFunc isMainN)
    {
        var verdict = isMainN(edgeId, localAccess);
        // Without a leftMain state-bit the bidirectional defaults unknown to "main": the per-half
        // rule then doesn't spuriously reject across edges we know nothing about.
        return verdict ?? true;
    }
}

/// <summary>
/// Notifies that a half has reached a state — settled it, or queued it.
/// </summary>
internal delegate void ReachedCallback(
    EdgeId edge, bool forward, byte? headOrder, uint pointer, double cost, VertexId vertex);

/// <summary>
/// Invoked for every relaxation, improving or not: filtering to improvements would describe a
/// tree rather than a graph. By value, since a path-tree pointer means nothing to an outside
/// caller, and without costs, which the two halves measure in opposite directions.
/// </summary>
/// <param name="turnVertex">Where the two edges meet, in this half's direction of travel.</param>
internal delegate void RelaxedCallback(EdgeId fromEdge, VertexId turnVertex, EdgeId toEdge);

/// <summary>
/// One arrival at a vertex, recorded so the other half can ask whether it may meet here.
/// </summary>
internal readonly record struct SettledEntry(
    EdgeId Edge, bool Forward, byte? HeadOrder, uint Pointer, double Cost);
