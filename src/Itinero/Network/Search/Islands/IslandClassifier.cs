using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// Pure graph-only island classification for a single seed edge.
///
/// Unlike the legacy <see cref="IslandBuilder"/>, this entry point has no
/// awareness of tiles, no DONE-tile fast-path and no shared per-network state.
/// It walks the routing network outward from the seed via vertex-BFS using its
/// <see cref="RoutingNetworkEdgeEnumerator"/>, consults / updates the caller via
/// <see cref="IIslandClassificationStore"/>, and returns the final classification
/// for the seed.
///
/// The tile-aware preprocessor that batches whole tiles and exploits the
/// "all edges in a tile are not-island" shortcut is layered on top of this in a
/// separate orchestration layer.
/// </summary>
public static class IslandClassifier
{
    /// <summary>
    /// Classifies a single edge against the routable main network.
    /// </summary>
    /// <summary>
    /// When non-null, every <see cref="ClassifyAsync"/> invocation emits a
    /// trace of what it walked: seed edge, tiles it loaded (with order), and
    /// final outcome. Set to <c>Console.Error.WriteLine</c> from tests / hosts
    /// that want to see classifier activity.
    /// </summary>
    public static System.Action<string>? Trace { get; set; }

    /// <summary>
    /// Hard cap on settled edges (edges ProcessEdge'd into the local dg)
    /// before <see cref="ClassifyAsync"/> bails out and treats the seed as
    /// island. Default 4096. Set to <see cref="int.MaxValue"/> to disable.
    /// This is a temporary stopgap for cases where a single classification
    /// would otherwise walk a huge component (e.g. a one-way edge with no
    /// nearby bidir cluster ≥ MaxIslandSize); the limit will go away once a
    /// principled bound is in place.
    /// </summary>
    public static int MaxSettledEdges { get; set; } = 4096;

    /// <summary>
    /// Per-run counters populated when <see cref="Trace"/> is non-null.
    /// Diagnostic only; instance per <see cref="ClassifyAsync"/> call.
    /// </summary>
    private sealed class Counters
    {
        public int CanGoToTrue, CanGoToFalse;
        public int CanComeFromTrue, CanComeFromFalse;
        public int LinksAdded;
        public int MergesPerformed;
        public int CollapseCalls;
    }

    public static async Task<IslandStatus> ClassifyAsync(
        RoutingNetwork network,
        Profile profile,
        EdgeId seed,
        IIslandClassificationStore store,
        CancellationToken cancellationToken)
    {
        var trace = Trace;
        var maxSettledEdges = MaxSettledEdges;
        var sw = trace == null ? null : System.Diagnostics.Stopwatch.StartNew();
        var tilesLoaded = trace == null ? null : new List<uint>();
        var counters = trace == null ? null : new Counters();
        var verticesVisited = 0;
        var edgesProcessed = 0;

        // fast-path: store already has the answer.
        var cached = store.Get(seed);
        if (cached != IslandStatus.Unknown)
        {
            trace?.Invoke($"[island-classify] seed={seed} profile={profile.Name} → CACHED {cached}");
            return cached;
        }

        var dg = new IslandDirectedGraph();
        var localIslands = new HashSet<EdgeId>();
        var maxIslandSize = network.IslandManager.MaxIslandSize;
        var costFunction = network.GetCostFunctionFor(profile);
        var edgeEnumerator = network.GetEdgeEnumerator();

        // Edge must exist in the network.
        if (!edgeEnumerator.MoveTo(seed, true))
        {
            trace?.Invoke($"[island-classify] seed={seed} profile={profile.Name} → UNKNOWN (edge not found in network)");
            return IslandStatus.Unknown;
        }

        // Edge must be traversable by this profile in at least one direction.
        // Otherwise it's not an "island" — it's just outside the profile's
        // network entirely (e.g. a pedestrian path classified under car.fast).
        // Returning Island and caching that would poison the store for any
        // future classification that touches this edge as a neighbour.
        var canForward = costFunction.GetIslandBuilderCost(edgeEnumerator);
        if (!edgeEnumerator.MoveTo(seed, false))
        {
            trace?.Invoke($"[island-classify] seed={seed} profile={profile.Name} → UNKNOWN (edge not found backward)");
            return IslandStatus.Unknown;
        }
        var canBackward = costFunction.GetIslandBuilderCost(edgeEnumerator);
        if (!canForward && !canBackward)
        {
            trace?.Invoke($"[island-classify] seed={seed} profile={profile.Name} → UNKNOWN (not traversable by profile in either direction)");
            return IslandStatus.Unknown;
        }

        // Position back on forward for the tail/head capture below.
        edgeEnumerator.MoveTo(seed, true);
        var seedTail = edgeEnumerator.Tail;
        var seedHead = edgeEnumerator.Head;
        trace?.Invoke($"[island-classify] BEGIN seed={seed} tile={seed.TileId} profile={profile.Name} maxIslandSize={maxIslandSize} tail={seedTail} head={seedHead} canForward={canForward} canBackward={canBackward}");

        // Edge-frontier BFS: starting from seed, ProcessEdge returns the set
        // of neighbours that got a directional link added. Only those get
        // enqueued. The walk stays inside seed's directional reachable
        // closure — it does NOT expand through vertex-shared edges that have
        // no canGoTo / canComeFrom relationship to seed's component.
        var enqueued = new HashSet<EdgeId> { seed };
        var frontier = new Queue<EdgeId>();
        frontier.Enqueue(seed);

        while (frontier.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested) return IslandStatus.Unknown;

            // Early-exit when seed graduates to main-net via a direct merge
            // OR is declared island locally. Cheap O(1) checks.
            if (dg.IsNotIsland(seed))
            {
                store.Set(seed, IslandStatus.NotIsland);
                trace?.Invoke($"[island-classify] END seed={seed} → NOTISLAND (merge during BFS) edges={edgesProcessed} elapsed={sw!.ElapsedMilliseconds}ms");
                return IslandStatus.NotIsland;
            }
            if (localIslands.Contains(seed))
            {
                store.Set(seed, IslandStatus.Island);
                trace?.Invoke($"[island-classify] END seed={seed} → ISLAND (declared during BFS) edges={edgesProcessed} elapsed={sw!.ElapsedMilliseconds}ms");
                return IslandStatus.Island;
            }

            var e = frontier.Dequeue();

            var linkedNeighbours = await ProcessEdgeAsync(network, dg, localIslands, store, costFunction,
                edgeEnumerator, e, maxIslandSize, counters, cancellationToken);
            edgesProcessed++;
            if (cancellationToken.IsCancellationRequested) return IslandStatus.Unknown;

            if (trace != null && edgesProcessed % 2000 == 0)
            {
                var (totalE, largestE, compsE, sentinelE) = dg.Stats();
                var (cFwd, cBwd) = dg.ReachMainNetworkDirections(seed);
                var (oC, iC, oM, iM) = dg.EdgeLinkStats(seed);
                trace($"[island-classify] PROGRESS edges={edgesProcessed} frontier={frontier.Count} dg.edges={totalE} largest={largestE} mainNet={sentinelE} seed.reach=(fwd:{cFwd},bwd:{cBwd}) seed.links=(out:{oC},in:{iC}) seed.directMain=(out:{oM},in:{iM}) elapsed={sw!.ElapsedMilliseconds}ms");
            }

            // Enqueue newly-discovered directional neighbours.
            foreach (var n in linkedNeighbours)
            {
                if (enqueued.Add(n)) frontier.Enqueue(n);
            }

            if (edgesProcessed >= maxSettledEdges)
            {
                if (trace != null)
                {
                    var (totalEdges, largest, components, sentinelHasMembers) = dg.Stats();
                    var (canFwd, canBwd) = dg.ReachMainNetworkDirections(seed);
                    var (outCount, inCount, outHasMain, inHasMain) = dg.EdgeLinkStats(seed);
                    trace($"[island-classify] END seed={seed} → UNKNOWN (LIMIT-HIT settled={edgesProcessed} cap={maxSettledEdges}) elapsed={sw!.ElapsedMilliseconds}ms");
                    trace($"[island-classify] LIMIT-HIT-DG-STATS dg.edges={totalEdges} components={components} largestComponent={largest} mainNetHasMembers={sentinelHasMembers} seed.reachMain=(fwd:{canFwd},bwd:{canBwd}) seed.localIsland={localIslands.Contains(seed)} seed.links=(out:{outCount},in:{inCount}) seed.directMain=(out:{outHasMain},in:{inHasMain})");
                    trace($"[island-classify] LIMIT-HIT-COUNTERS canGoTo(true={counters!.CanGoToTrue},false={counters.CanGoToFalse}) canComeFrom(true={counters.CanComeFromTrue},false={counters.CanComeFromFalse}) linksAdded={counters.LinksAdded} merges={counters.MergesPerformed} collapses={counters.CollapseCalls}");
                    trace($"[island-classify] LIMIT-HIT-GEOMETRY seed={seed} {EdgeGeoJson(network, seed)}");
                }
                return IslandStatus.Unknown;
            }

            // Mid-BFS resolution: only the monotonic steps (SCC merge +
            // main-network reachability). Cheap to run after every edge
            // process since the BFS is now bounded to seed's closure.
            var candidates = CollectAllCandidates(dg, localIslands);
            TryResolve(dg, localIslands, store, candidates, maxIslandSize);
        }

        // frontier exhausted — final attempt to resolve over every known edge.
        var finalCandidates = CollectAllCandidates(dg, localIslands);
        TryResolve(dg, localIslands, store, finalCandidates, maxIslandSize);

        if (dg.IsNotIsland(seed))
        {
            store.Set(seed, IslandStatus.NotIsland);
            trace?.Invoke($"[island-classify] END seed={seed} → NOTISLAND (final TryResolve) visited={verticesVisited} tiles={tilesLoaded!.Count} edges={edgesProcessed} elapsed={sw!.ElapsedMilliseconds}ms");
            return IslandStatus.NotIsland;
        }
        if (localIslands.Contains(seed))
        {
            store.Set(seed, IslandStatus.Island);
            trace?.Invoke($"[island-classify] END seed={seed} → ISLAND (final TryResolve) visited={verticesVisited} tiles={tilesLoaded!.Count} edges={edgesProcessed} elapsed={sw!.ElapsedMilliseconds}ms");
            return IslandStatus.Island;
        }

        // bounded component, never graduated, never reached main → island.
        // Notify every still-undeclared member of the seed's component.
        if (dg.IsInGraph(seed))
        {
            var members = dg.GetMembers(dg.Find(seed));
            if (members != null)
            {
                var snapshot = new List<EdgeId>(members);
                foreach (var m in snapshot)
                {
                    if (localIslands.Add(m))
                        store.Set(m, IslandStatus.Island);
                }
            }
        }
        if (!localIslands.Contains(seed))
        {
            localIslands.Add(seed);
            store.Set(seed, IslandStatus.Island);
        }
        trace?.Invoke($"[island-classify] END seed={seed} → ISLAND (bounded component) visited={verticesVisited} tiles={tilesLoaded!.Count} edges={edgesProcessed} elapsed={sw!.ElapsedMilliseconds}ms");
        return IslandStatus.Island;
    }

    /// <summary>
    /// Returns a GeoJSON Feature for the edge's geometry. Trace helper only;
    /// caller must guard with <c>trace != null</c>.
    /// </summary>
    private static string EdgeGeoJson(RoutingNetwork network, EdgeId edgeId)
    {
        var en = network.GetEdgeEnumerator();
        if (!en.MoveTo(edgeId, true)) return "{}";
        var sb = new System.Text.StringBuilder();
        sb.Append("{\"type\":\"Feature\",\"properties\":{\"edgeId\":\"");
        sb.Append(edgeId);
        sb.Append("\",\"tileId\":");
        sb.Append(edgeId.TileId);
        sb.Append("},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[");
        var first = true;
        foreach (var c in en.GetCompleteShape())
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('[');
            sb.Append(c.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(c.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(']');
        }
        sb.Append("]}}");
        return sb.ToString();
    }

    /// <summary>
    /// Processes a single edge into the dg: builds directed links at both
    /// endpoints, merges into bidirectionally-connected neighbours, escalates
    /// to MainNetworkSentinel when the merged component reaches MaxIslandSize.
    ///
    /// Returns the set of neighbour <see cref="EdgeId"/>s that got a directional
    /// link added (canGoTo or canComeFrom passed). Caller uses this set as the
    /// edge-frontier — only edges in seed's directional reach get enqueued, so
    /// the BFS doesn't expand through vertex-shared edges that have no
    /// directional bearing on seed.
    /// </summary>
    private static async Task<List<EdgeId>> ProcessEdgeAsync(
        RoutingNetwork network,
        IslandDirectedGraph dg,
        HashSet<EdgeId> localIslands,
        IIslandClassificationStore store,
        ICostFunction costFunction,
        RoutingNetworkEdgeEnumerator edgeEnumerator,
        EdgeId edgeId,
        int maxIslandSize,
        Counters? counters,
        CancellationToken cancellationToken)
    {
        var linkedNeighbours = new List<EdgeId>();
        if (dg.IsProcessed(edgeId)) return linkedNeighbours;
        if (dg.IsNotIsland(edgeId)) return linkedNeighbours;
        if (localIslands.Contains(edgeId)) return linkedNeighbours;

        // seed from store for this edge directly.
        var cached = store.Get(edgeId);
        if (cached == IslandStatus.NotIsland)
        {
            dg.AddVertex(edgeId);
            dg.CollapseToMainNetwork(edgeId);
            dg.SetProcessed(edgeId);
            return linkedNeighbours;
        }
        if (cached == IslandStatus.Island)
        {
            localIslands.Add(edgeId);
            return linkedNeighbours;
        }

        if (!edgeEnumerator.MoveTo(edgeId, true)) return linkedNeighbours;
        var canForward = costFunction.GetIslandBuilderCost(edgeEnumerator);
        if (!edgeEnumerator.MoveTo(edgeId, false)) return linkedNeighbours;
        var canBackward = costFunction.GetIslandBuilderCost(edgeEnumerator);

        if (!canForward && !canBackward) return linkedNeighbours;

        dg.AddVertex(edgeId);

        for (var pass = 0; pass < 2; pass++)
        {
            var forward = pass == 0;
            // Run BOTH passes regardless of canForward/canBackward. For a one-
            // way edge the "non-traversable" pass still needs to enumerate at
            // the other endpoint to discover INCOMING neighbours: canGoTo will
            // fail (edge can't leave that vertex), but canComeFrom can pass
            // (neighbour leads INTO this edge in its allowed direction). That
            // captures the in-link side of the directional closure, which the
            // edge-frontier BFS needs to find seeds reachable FROM the main
            // network.

            edgeEnumerator.MoveTo(edgeId, forward);
            var targetVertex = edgeEnumerator.Head;

            // Pre-positioned helper enumerators for the two-enumerator
            // GetIslandBuilderCost primitive at the shared vertex.
            var edgeIdFrom = edgeEnumerator.Network.GetEdgeEnumerator();
            edgeIdFrom.MoveTo(edgeId, forward);
            var edgeIdTo = edgeEnumerator.Network.GetEdgeEnumerator();
            edgeIdTo.MoveTo(edgeId, !forward);

            var neighborArriving = edgeEnumerator.Network.GetEdgeEnumerator();

            // targetVertex may live in a tile we haven't loaded yet (boundary
            // edge head). Enumerating without notifying silently misses every
            // neighbor in that tile — leaving directed links incomplete and
            // making CanReachMainNetwork false-negative. Notify first.
            await network.UsageNotifier.NotifyVertex(network, targetVertex, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return linkedNeighbours;

            if (!edgeEnumerator.MoveTo(targetVertex)) continue;

            while (edgeEnumerator.MoveNext())
            {
                if (edgeEnumerator.EdgeId == edgeId) continue;

                var neighborId = edgeEnumerator.EdgeId;

                // skip known islands (local + store).
                if (localIslands.Contains(neighborId)) continue;
                var neighborCached = store.Get(neighborId);
                if (neighborCached == IslandStatus.Island)
                {
                    localIslands.Add(neighborId);
                    continue;
                }

                // Capture Forward BEFORE any cost-function call: GetIslandBuilderCost
                // may MoveTo the enumerator internally, flipping Forward and breaking
                // the next MoveTo(neighborId, !Forward) for canComeFrom.
                var iterationForward = edgeEnumerator.Forward;

                var canGoTo = costFunction.GetIslandBuilderCost(edgeIdFrom, edgeEnumerator);
                if (counters != null) { if (canGoTo) counters.CanGoToTrue++; else counters.CanGoToFalse++; }

                neighborArriving.MoveTo(neighborId, !iterationForward);
                var canComeFrom = costFunction.GetIslandBuilderCost(neighborArriving, edgeIdTo);
                if (counters != null) { if (canComeFrom) counters.CanComeFromTrue++; else counters.CanComeFromFalse++; }

                // No directional connection from edgeId to this neighbour — it
                // does NOT belong to edgeId's directional closure. Skip
                // entirely: don't AddVertex, don't enqueue. This is the core
                // edge-frontier discipline that keeps the BFS bounded by
                // seed's reachable subgraph instead of the whole vertex
                // component.
                if (!canGoTo && !canComeFrom)
                {
                    edgeEnumerator.MoveTo(targetVertex);
                    while (edgeEnumerator.MoveNext())
                    {
                        if (edgeEnumerator.EdgeId == neighborId) break;
                    }
                    continue;
                }

                // determine dg vertex for the neighbor; if pre-classified as
                // NotIsland the neighbor is treated as the main-network sentinel.
                EdgeId neighborDgVertex;
                if (neighborCached == IslandStatus.NotIsland)
                {
                    if (!dg.IsInGraph(neighborId))
                    {
                        dg.AddVertex(neighborId);
                        dg.CollapseToMainNetwork(neighborId);
                    }
                    neighborDgVertex = IslandDirectedGraph.MainNetworkSentinel;
                }
                else if (dg.IsNotIsland(neighborId))
                {
                    neighborDgVertex = IslandDirectedGraph.MainNetworkSentinel;
                }
                else
                {
                    dg.AddVertex(neighborId);
                    neighborDgVertex = neighborId;
                }

                if (canGoTo)
                {
                    dg.AddDirectedLink(edgeId, neighborDgVertex);
                    if (counters != null) counters.LinksAdded++;
                    if (dg.HasDirectedLink(neighborDgVertex, edgeId))
                    {
                        MergeAndMaybeCollapse(dg, store, edgeId, neighborDgVertex, maxIslandSize, counters);
                    }
                }

                if (canComeFrom)
                {
                    dg.AddDirectedLink(neighborDgVertex, edgeId);
                    if (counters != null) counters.LinksAdded++;
                    if (dg.HasDirectedLink(edgeId, neighborDgVertex))
                    {
                        MergeAndMaybeCollapse(dg, store, edgeId, neighborDgVertex, maxIslandSize, counters);
                    }
                }

                // Track this neighbour as part of the directional frontier so
                // the caller can enqueue it for ProcessEdge. Skip the sentinel —
                // it's already terminal, no further exploration needed.
                if (neighborDgVertex != IslandDirectedGraph.MainNetworkSentinel)
                    linkedNeighbours.Add(neighborId);

                if (dg.IsNotIsland(edgeId)) break;

                edgeEnumerator.MoveTo(targetVertex);
                while (edgeEnumerator.MoveNext())
                {
                    if (edgeEnumerator.EdgeId == neighborId) break;
                }
            }

            if (dg.IsNotIsland(edgeId)) break;
        }

        dg.SetProcessed(edgeId);
        return linkedNeighbours;
    }

    private static void MergeAndMaybeCollapse(IslandDirectedGraph dg,
        IIslandClassificationStore store, EdgeId a, EdgeId b, int maxIslandSize, Counters? counters = null)
    {
        dg.Merge(a, b);
        if (counters != null) counters.MergesPerformed++;
        var newSize = dg.GetSize(dg.Find(a));
        if (newSize >= maxIslandSize)
        {
            CollapseAndNotify(dg, store, a);
            if (counters != null) counters.CollapseCalls++;
        }
    }

    /// <summary>
    /// Snapshots the members of the edge's component, collapses to main
    /// network, then notifies the store that each member is NotIsland.
    /// </summary>
    private static void CollapseAndNotify(IslandDirectedGraph dg,
        IIslandClassificationStore store, EdgeId edgeOrRoot)
    {
        var root = dg.Find(edgeOrRoot);
        if (root == IslandDirectedGraph.MainNetworkSentinel) return;
        var members = dg.GetMembers(root);
        var snapshot = members != null ? new List<EdgeId>(members) : null;
        dg.CollapseToMainNetwork(edgeOrRoot);
        if (snapshot != null)
        {
            foreach (var m in snapshot) store.Set(m, IslandStatus.NotIsland);
        }
    }

    /// <summary>
    /// Resolves seed-component candidates via dead-end pruning, Tarjan SCC
    /// detection + merge, and bidirectional main-network reachability.
    /// </summary>
    private static void TryResolve(IslandDirectedGraph dg,
        HashSet<EdgeId> localIslands,
        IIslandClassificationStore store,
        List<EdgeId> candidates,
        int maxIslandSize)
    {
        bool changed;
        do
        {
            changed = false;

            // NOTE: no dead-end pruning. A component looks like a dead-end
            // (no incoming OR no outgoing) only because the walk hasn't yet
            // discovered the edge or vertex on the other side of the missing
            // direction. With persistent caching, bounded walks and a
            // potentially-global routing graph, we never have proof the graph
            // is fully explored, so dead-end pruning is unsafe. Components
            // that are truly trapped get caught by the bounded-component
            // fallback after the frontier exhausts — they have a Find()-root
            // that never merged into MainNet, and we declare every member
            // Island there.

            // 2. Tarjan SCC merge among remaining candidates.
            if (candidates.Count >= 2)
            {
                if (dg.DetectAndMergeCycles(candidates))
                {
                    changed = true;
                    for (var i = candidates.Count - 1; i >= 0; i--)
                    {
                        var edgeId = candidates[i];
                        if (!dg.IsInGraph(edgeId) || dg.IsNotIsland(edgeId))
                        {
                            candidates.RemoveAt(i);
                            continue;
                        }

                        var root = dg.Find(edgeId);
                        if (dg.GetSize(root) >= maxIslandSize)
                        {
                            CollapseAndNotify(dg, store, root);
                            candidates.RemoveAt(i);
                        }
                    }
                }
            }

            // 3. bidirectional main-network reachability.
            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                var edgeId = candidates[i];
                if (!dg.IsInGraph(edgeId) || dg.IsNotIsland(edgeId))
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                if (dg.CanReachMainNetwork(edgeId))
                {
                    CollapseAndNotify(dg, store, edgeId);
                    candidates.RemoveAt(i);
                    changed = true;
                }
            }
        } while (changed);
    }

    /// <summary>
    /// Returns every edge currently in <paramref name="dg"/> that hasn't been
    /// resolved (neither graduated to main network nor declared island). Used
    /// as the candidate set for the global resolution pass so that one-way
    /// cycle members in separate components can all participate in Tarjan SCC.
    /// </summary>
    private static List<EdgeId> CollectAllCandidates(IslandDirectedGraph dg,
        HashSet<EdgeId> localIslands)
    {
        var all = dg.GetAllEdges();
        var candidates = new List<EdgeId>(all.Count);
        foreach (var e in all)
        {
            if (dg.IsNotIsland(e)) continue;
            if (localIslands.Contains(e)) continue;
            candidates.Add(e);
        }
        return candidates;
    }
}
