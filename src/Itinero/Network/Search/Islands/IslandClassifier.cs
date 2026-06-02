using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Tiles;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// Per-edge island classifier. See <c>docs/island-detection-algorithm.md</c>
/// in publish-api for the spec. Short version:
///
/// We answer two reachability questions for the seed in the dg:
///   - Forward:  is there a path  seed ↝ sentinel  (seed reaches MainNet)?
///   - Backward: is there a path  sentinel ↝ seed  (MainNet reaches seed)?
///
/// NotIsland iff both are yes; Island iff either is no. Two BFS searches
/// (forward + backward) run concurrently. After every <see cref="IslandDirectedGraph.AddDirectedLink"/>
/// we propagate two per-component sticky sets:
///   - <c>inForward</c>  — components reachable from seed via <c>_outgoing</c>.
///   - <c>inBackward</c> — components reachable from seed via <c>_incoming</c>.
/// Each component enters each set at most once, so total propagation across
/// the classification is O(closure-size).
///
/// Termination:
///   - sentinel ∈ inForward ∩ inBackward  →  NotIsland.
///   - forwardQueue empty ∧ sentinel ∉ inForward  →  Island.
///   - backwardQueue empty ∧ sentinel ∉ inBackward →  Island.
/// </summary>
public static class IslandClassifier
{
    public static async Task<IslandStatus> ClassifyAsync(
        RoutingNetwork network,
        Profile profile,
        EdgeId seed,
        CancellationToken cancellationToken,
        IslandKind kind = IslandKind.Full)
    {
        var islands = network.IslandManager.GetIslandsFor(profile);
        var dg = network.IslandManager.GetOrCreateDirectedGraph(profile, kind);
        var maxIslandSize = network.IslandManager.MaxIslandSize;
        var costFunction = IslandKindCostFunctions.GetFor(network, profile, kind);
        var probe = network.GetEdgeEnumerator();
        var sentinel = IslandDirectedGraph.MainNetworkSentinel;

        // Pre-population secondary oracle for Full: any edge in the NonLocal
        // MainNet sentinel is guaranteed to be in Full MainNet too, since
        // N-only paths are valid Full paths.
        IslandDirectedGraph? nonLocalDg = null;
        if (kind == IslandKind.Full)
        {
            nonLocalDg = network.IslandManager.GetOrCreateDirectedGraph(profile, IslandKind.NonLocal);
        }

        // Oracle / cached-state short-circuits.
        if (IsKnownIsland(seed, kind, islands)) return IslandStatus.Island;
        if (dg.IsNotIsland(seed)) return IslandStatus.NotIsland;
        if (nonLocalDg != null && nonLocalDg.IsNotIsland(seed))
        {
            dg.AddVertex(seed);
            dg.CollapseToMainNetwork(seed);
            return IslandStatus.NotIsland;
        }

        // Edge must exist and be traversable in at least one direction.
        if (!probe.MoveTo(seed, true)) return IslandStatus.Unknown;
        var seedHead = probe.Head;
        var seedTail = probe.Tail;
        var canFwd = costFunction.GetIslandBuilderCost(probe);
        if (!probe.MoveTo(seed, false)) return IslandStatus.Unknown;
        var canBwd = costFunction.GetIslandBuilderCost(probe);
        if (!canFwd && !canBwd) return IslandStatus.Unknown;

        // Set up. Seed starts in both F and B (it is trivially reachable from itself in either direction).
        dg.AddVertex(seed);
        var ctx = new Ctx(network, dg, nonLocalDg, kind, islands, costFunction, maxIslandSize);
        ctx.InForward.Add(dg.Find(seed));
        ctx.InBackward.Add(dg.Find(seed));

        // Process seed at both endpoints. Each AddDirectedLink inside
        // ProcessAtEndpointAsync updates inForward/inBackward and enqueues
        // newly-marked edges into the right queue.
        await ProcessAtEndpointAsync(seed, seedHead, ctx, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return IslandStatus.Unknown;
        await ProcessAtEndpointAsync(seed, seedTail, ctx, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return IslandStatus.Unknown;
        dg.SetProcessed(seed);

        // Main loop: termination checks are O(1) lookups on the sentinel.
        while (true)
        {
            // Graduation: if seed was absorbed into the sentinel by an eager
            // cycle-merge or size-threshold collapse, return immediately.
            if (dg.IsNotIsland(seed)) return IslandStatus.NotIsland;

            var sentinelInF = ctx.InForward.Contains(sentinel);
            var sentinelInB = ctx.InBackward.Contains(sentinel);
            if (sentinelInF && sentinelInB)
            {
                dg.CollapseToMainNetwork(seed);
                return IslandStatus.NotIsland;
            }
            if (ctx.ForwardQueue.Count == 0 && !sentinelInF)
            {
                islands.SetEdgeOnIsland(seed, kind);
                return IslandStatus.Island;
            }
            if (ctx.BackwardQueue.Count == 0 && !sentinelInB)
            {
                islands.SetEdgeOnIsland(seed, kind);
                return IslandStatus.Island;
            }

            if (ctx.ForwardQueue.Count > 0)
            {
                var e = ctx.ForwardQueue.Dequeue();
                await ProcessEdgeAsync(e, ctx, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return IslandStatus.Unknown;
            }
            if (ctx.BackwardQueue.Count > 0)
            {
                var e = ctx.BackwardQueue.Dequeue();
                await ProcessEdgeAsync(e, ctx, cancellationToken);
                if (cancellationToken.IsCancellationRequested) return IslandStatus.Unknown;
            }
        }
    }

    public static async Task BuildForTileAsync(
        RoutingNetwork network,
        Profile profile,
        uint tileId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var islands = network.IslandManager.GetIslandsFor(profile);
        if (islands.GetTileDone(tileId)) return;

        // Use the Full cost function to enumerate traversable edges and to
        // detect L-tagged ones (NonLocalCostFunction masks L away — we need
        // the raw tag here for both edge-gathering and L-set computation).
        var fullCostFunction = IslandKindCostFunctions.GetFor(network, profile, IslandKind.Full);
        await network.UsageNotifier.NotifyVertex(network, new VertexId(tileId, 0), cancellationToken);
        if (cancellationToken.IsCancellationRequested) return;
        var tile = network.GetTileForRead(tileId);
        if (tile == null) return;

        var probe = network.GetEdgeEnumerator();
        var tileEnum = new NetworkTileEnumerator();
        tileEnum.MoveTo(tile);
        var v = new VertexId(tileId, 0);
        var edges = new List<EdgeId>();
        var lEdges = new HashSet<EdgeId>();
        while (tileEnum.MoveTo(v))
        {
            while (tileEnum.MoveNext())
            {
                if (!tileEnum.Forward) continue;
                var edgeId = tileEnum.EdgeId;
                if (!probe.MoveTo(edgeId, true)) continue;
                var fwd = fullCostFunction.Get(probe, true);
                var canFwd = fwd is { canAccess: true, turnCost: < double.MaxValue };
                if (!probe.MoveTo(edgeId, false)) continue;
                var bwd = fullCostFunction.Get(probe, true);
                var canBwd = bwd is { canAccess: true, turnCost: < double.MaxValue };
                if (canFwd || canBwd) edges.Add(edgeId);
                if (fwd.localAccess || bwd.localAccess) lEdges.Add(edgeId);
            }
            v = new VertexId(tileId, v.LocalId + 1);
        }

        // NonLocal pass first — classifies the N-only subgraph. L-tagged
        // seeds are masked out by NonLocalCostFunction and return Unknown.
        foreach (var edge in edges)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await ClassifyAsync(network, profile, edge, cancellationToken, IslandKind.NonLocal);
        }

        // Full pass — reuses NonLocal MainNet as a positive oracle to short-
        // circuit edges already known to be in N-mainland.
        foreach (var edge in edges)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await ClassifyAsync(network, profile, edge, cancellationToken, IslandKind.Full);
        }

        // Locals = NonLocal-Island ∩ Full-NotIsland ∩ non-L. These are the
        // non-L edges only reachable from main-N through an L-edge first.
        // L-tagged edges and Full-Island edges are excluded.
        foreach (var edge in edges)
        {
            if (lEdges.Contains(edge)) continue;
            if (islands.IsEdgeOnIsland(edge)) continue;
            if (!islands.IsEdgeOnIsland(edge, IslandKind.NonLocal)) continue;
            islands.SetEdgeLocal(edge);
        }

        islands.ClearNonLocalIslandEdges();
        islands.SetTileDone(tileId);
    }

    /// <summary>
    /// Kind-aware "known island" oracle. For Full this is the persistent
    /// Full-Islands set. For NonLocal an edge is known to be a NonLocal-Island
    /// if it is in Full-Islands (Full-Island ⟹ NonLocal-Island), in
    /// <c>_localEdges</c> (Local edges are unreachable via N-only paths), or
    /// in the transient <c>_nonLocalIslandEdges</c> set written during the
    /// current NonLocal pass.
    /// </summary>
    private static bool IsKnownIsland(EdgeId edgeId, IslandKind kind, Islands islands)
    {
        if (kind == IslandKind.NonLocal)
        {
            if (islands.IsEdgeOnIsland(edgeId)) return true;
            if (islands.IsEdgeLocal(edgeId)) return true;
            return islands.IsEdgeOnIsland(edgeId, IslandKind.NonLocal);
        }
        return islands.IsEdgeOnIsland(edgeId);
    }

    /// <summary>
    /// Per-classification working state. Owns the two queues, the per-queue
    /// dedup sets, and the F/B sticky sets used to track per-component
    /// reachability to/from the seed in the dg.
    /// </summary>
    private sealed class Ctx
    {
        public readonly RoutingNetwork Network;
        public readonly IslandDirectedGraph Dg;
        public readonly IslandDirectedGraph? NonLocalDg;
        public readonly IslandKind Kind;
        public readonly Islands Islands;
        public readonly ICostFunction CostFunction;
        public readonly int MaxIslandSize;
        public readonly Queue<EdgeId> ForwardQueue = new();
        public readonly Queue<EdgeId> BackwardQueue = new();
        public readonly HashSet<EdgeId> QueuedForward = new();
        public readonly HashSet<EdgeId> QueuedBackward = new();
        public readonly HashSet<EdgeId> InForward = new();
        public readonly HashSet<EdgeId> InBackward = new();

        public Ctx(RoutingNetwork network, IslandDirectedGraph dg,
            IslandDirectedGraph? nonLocalDg, IslandKind kind, Islands islands,
            ICostFunction costFunction, int maxIslandSize)
        {
            Network = network;
            Dg = dg;
            NonLocalDg = nonLocalDg;
            Kind = kind;
            Islands = islands;
            CostFunction = costFunction;
            MaxIslandSize = maxIslandSize;
        }
    }

    private static async Task ProcessEdgeAsync(EdgeId edgeId, Ctx ctx, CancellationToken cancellationToken)
    {
        if (ctx.Dg.IsProcessed(edgeId)) return;
        var probe = ctx.Network.GetEdgeEnumerator();
        if (!probe.MoveTo(edgeId, true)) { ctx.Dg.SetProcessed(edgeId); return; }
        var head = probe.Head;
        var tail = probe.Tail;
        await ProcessAtEndpointAsync(edgeId, head, ctx, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return;
        await ProcessAtEndpointAsync(edgeId, tail, ctx, cancellationToken);
        ctx.Dg.SetProcessed(edgeId);
    }

    private static async Task ProcessAtEndpointAsync(
        EdgeId edgeId, VertexId vertex, Ctx ctx, CancellationToken cancellationToken)
    {
        await ctx.Network.UsageNotifier.NotifyVertex(ctx.Network, vertex, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return;

        var edgeIdFrom = ctx.Network.GetEdgeEnumerator();
        if (!edgeIdFrom.MoveTo(edgeId, true)) return;
        var arrivalForward = edgeIdFrom.Head == vertex;
        if (!arrivalForward)
        {
            if (!edgeIdFrom.MoveTo(edgeId, false)) return;
        }
        var edgeIdTo = ctx.Network.GetEdgeEnumerator();
        if (!edgeIdTo.MoveTo(edgeId, !arrivalForward)) return;
        var neighborArriving = ctx.Network.GetEdgeEnumerator();

        var enumerator = ctx.Network.GetEdgeEnumerator();
        if (!enumerator.MoveTo(vertex)) return;
        while (enumerator.MoveNext())
        {
            if (enumerator.EdgeId == edgeId) continue;
            var neighborId = enumerator.EdgeId;
            var iterationForward = enumerator.Forward;

            var canGoTo = ctx.CostFunction.GetIslandBuilderCost(edgeIdFrom, enumerator);
            neighborArriving.MoveTo(neighborId, !iterationForward);
            var canComeFrom = ctx.CostFunction.GetIslandBuilderCost(neighborArriving, edgeIdTo);

            enumerator.MoveTo(vertex);
            while (enumerator.MoveNext())
            {
                if (enumerator.EdgeId == neighborId) break;
            }

            if (!canGoTo && !canComeFrom) continue;

            // Oracle.
            EdgeId neighborDgVertex;
            bool isKnown;
            if (IsKnownIsland(neighborId, ctx.Kind, ctx.Islands))
            {
                ctx.Dg.AddVertex(neighborId);
                neighborDgVertex = neighborId;
                isKnown = true;
            }
            else if (ctx.Dg.IsNotIsland(neighborId) ||
                     (ctx.NonLocalDg != null && ctx.NonLocalDg.IsNotIsland(neighborId)) ||
                     ctx.Islands.GetTileDone(neighborId.TileId))
            {
                neighborDgVertex = IslandDirectedGraph.MainNetworkSentinel;
                isKnown = true;
            }
            else
            {
                ctx.Dg.AddVertex(neighborId);
                neighborDgVertex = neighborId;
                isKnown = false;
            }

            if (canGoTo) AddLinkAndPropagate(edgeId, neighborDgVertex, ctx);
            if (canComeFrom) AddLinkAndPropagate(neighborDgVertex, edgeId, ctx);

            // The neighbour's queue assignment is handled by the propagation
            // (newly inForward → forwardQueue; newly inBackward → backwardQueue).
            // For known-island neighbours nothing needs to be queued; the dg
            // link still got added so cycle detection sees it.
            _ = isKnown;
        }
    }

    /// <summary>
    /// Adds a directed link <c>a → b</c> to the dg and incrementally maintains
    /// the per-classification <see cref="Ctx.InForward"/> and
    /// <see cref="Ctx.InBackward"/> sets through any propagation the new link
    /// causes. Newly-marked components are enqueued to the appropriate queue.
    /// </summary>
    private static void AddLinkAndPropagate(EdgeId a, EdgeId b, Ctx ctx)
    {
        var aRootBefore = ctx.Dg.Find(a);
        var bRootBefore = ctx.Dg.Find(b);
        if (aRootBefore == bRootBefore) return;

        // Capture F/B membership of both endpoints BEFORE the link is added.
        // If the call causes a cycle-merge, the original roots disappear and
        // we'll need to consolidate.
        var aInF = ctx.InForward.Contains(aRootBefore);
        var bInF = ctx.InForward.Contains(bRootBefore);
        var aInB = ctx.InBackward.Contains(aRootBefore);
        var bInB = ctx.InBackward.Contains(bRootBefore);

        var merged = ctx.Dg.AddDirectedLink(a, b);
        if (merged) MaybeCollapse(ctx.Dg, a, ctx.MaxIslandSize);

        if (merged)
        {
            // Cycle close: a and b (and possibly others) are now one component.
            // Combine F/B membership into the new root and propagate.
            RekeyAfterMerge(ctx);
            var newRoot = ctx.Dg.Find(a);
            var nowInF = aInF || bInF;
            var nowInB = aInB || bInB;

            if (nowInF)
            {
                ctx.InForward.Add(newRoot);
                // The merge can absorb previously-isolated components whose
                // members were never queued (because they weren't in F yet).
                // Re-enqueue idempotently — `queuedForward` dedups.
                EnqueueMembers(newRoot, ctx.ForwardQueue, ctx.QueuedForward, ctx);
                // Outgoing chains from the new root may lead to components
                // that weren't in F before; propagate into each.
                foreach (var t in ctx.Dg.GetOutgoingRoots(newRoot))
                {
                    if (!ctx.InForward.Contains(ctx.Dg.Find(t))) PropagateForward(t, ctx);
                }
            }
            if (nowInB)
            {
                ctx.InBackward.Add(newRoot);
                EnqueueMembers(newRoot, ctx.BackwardQueue, ctx.QueuedBackward, ctx);
                foreach (var s in ctx.Dg.GetIncomingRoots(newRoot))
                {
                    if (!ctx.InBackward.Contains(ctx.Dg.Find(s))) PropagateBackward(s, ctx);
                }
            }
        }
        else
        {
            // Plain link added (no merge). Propagate F/B across it if applicable.
            //   - If a was in F, b's outgoing closure joins F.
            //   - If b was in B, a's incoming closure joins B.
            if (aInF && !bInF) PropagateForward(bRootBefore, ctx);
            if (bInB && !aInB) PropagateBackward(aRootBefore, ctx);
        }
    }

    /// <summary>
    /// BFS from <paramref name="fromRoot"/> through <c>_outgoing</c> chains,
    /// marking newly-reached components as <c>inForward</c> and enqueueing
    /// their unprocessed members to the forward queue. Each component enters
    /// <c>inForward</c> at most once.
    /// </summary>
    private static void PropagateForward(EdgeId fromRoot, Ctx ctx)
    {
        var sentinel = IslandDirectedGraph.MainNetworkSentinel;
        var stack = new Stack<EdgeId>();
        stack.Push(fromRoot);
        while (stack.Count > 0)
        {
            var current = ctx.Dg.Find(stack.Pop());
            if (!ctx.InForward.Add(current)) continue;

            if (current != sentinel)
            {
                EnqueueMembers(current, ctx.ForwardQueue, ctx.QueuedForward, ctx);
            }

            foreach (var t in ctx.Dg.GetOutgoingRoots(current))
            {
                if (!ctx.InForward.Contains(ctx.Dg.Find(t))) stack.Push(t);
            }
        }
    }

    /// <summary>
    /// Mirror of <see cref="PropagateForward"/> walking <c>_incoming</c>.
    /// </summary>
    private static void PropagateBackward(EdgeId fromRoot, Ctx ctx)
    {
        var sentinel = IslandDirectedGraph.MainNetworkSentinel;
        var stack = new Stack<EdgeId>();
        stack.Push(fromRoot);
        while (stack.Count > 0)
        {
            var current = ctx.Dg.Find(stack.Pop());
            if (!ctx.InBackward.Add(current)) continue;

            if (current != sentinel)
            {
                EnqueueMembers(current, ctx.BackwardQueue, ctx.QueuedBackward, ctx);
            }

            foreach (var s in ctx.Dg.GetIncomingRoots(current))
            {
                if (!ctx.InBackward.Contains(ctx.Dg.Find(s))) stack.Push(s);
            }
        }
    }

    private static void EnqueueMembers(EdgeId root, Queue<EdgeId> queue, HashSet<EdgeId> queued, Ctx ctx)
    {
        var members = ctx.Dg.GetMembers(root);
        if (members == null) return;
        foreach (var m in members)
        {
            if (ctx.Dg.IsProcessed(m)) continue;
            // Don't queue known-island members. They were added to the dg only
            // so cycle detection sees them; we never expand through them.
            if (IsKnownIsland(m, ctx.Kind, ctx.Islands)) continue;
            if (!queued.Add(m)) continue;
            queue.Enqueue(m);
        }
    }

    /// <summary>
    /// Re-canonicalise the F and B sets after a merge. Each set's entries are
    /// component roots; a merge can collapse multiple of them into one. We
    /// replace stale entries with their current <see cref="Find"/> result and
    /// dedup.
    /// </summary>
    private static void RekeyAfterMerge(Ctx ctx)
    {
        Rekey(ctx.InForward, ctx.Dg);
        Rekey(ctx.InBackward, ctx.Dg);
    }

    private static void Rekey(HashSet<EdgeId> set, IslandDirectedGraph dg)
    {
        if (set.Count == 0) return;
        var fresh = new HashSet<EdgeId>(set.Count);
        foreach (var r in set) fresh.Add(dg.Find(r));
        if (fresh.Count == set.Count && SetEquals(fresh, set)) return;
        set.Clear();
        foreach (var r in fresh) set.Add(r);
    }

    private static bool SetEquals(HashSet<EdgeId> a, HashSet<EdgeId> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var x in a) if (!b.Contains(x)) return false;
        return true;
    }

    private static void MaybeCollapse(IslandDirectedGraph dg, EdgeId a, int maxIslandSize)
    {
        var size = dg.GetSize(dg.Find(a));
        if (size >= maxIslandSize) dg.CollapseToMainNetwork(a);
    }
}
