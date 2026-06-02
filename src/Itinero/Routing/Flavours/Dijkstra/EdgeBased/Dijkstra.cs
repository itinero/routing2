using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Routes.Paths;
using Itinero.Routing.DataStructures;
using Itinero.Snapping;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]

namespace Itinero.Routing.Flavours.Dijkstra.EdgeBased;

/// <summary>
/// An edge-based dijkstra implementation.
///
/// When <c>isMainN</c> is supplied to <see cref="RunAsync"/>, the search is access-aware:
/// each label carries a sticky <c>leftMain</c> bit that flips true on the first
/// main → non-main transition, after which the search refuses to relax onto any
/// edge classified as main-N. This enforces the "L-edge is fine iff it is the
/// only route to destination/origin" rule structurally — main-N may only host
/// a contiguous middle segment of the path. See the design note for the five
/// valid path shapes this admits.
///
/// When <c>isMainN</c> is null the search reduces to the classic edge-based
/// Dijkstra: <c>leftMain</c> stays false everywhere and no relaxations are
/// rejected on access grounds.
/// </summary>
internal class Dijkstra
{
    private readonly PathTree _tree = new();
    private readonly HashSet<(EdgeId edgeId, VertexId vertexId, bool leftMain)> _visits = new();
    private readonly BinaryHeap<(uint pointer, EdgeId edgeId, VertexId vertexId, bool leftMain)> _heap = new();

    public async Task<(Path? path, double cost)> RunAsync(RoutingNetwork network, SnapPoint source,
        SnapPoint target,
        DijkstraWeightFunc getDijkstraWeight,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? settled = null,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? queued = null,
        CancellationToken cancellationToken = default,
        IsMainNFunc? isMainN = null)
    {
        var paths = await this.RunAsync(network, (source, null), new[] { (target, (bool?)null) }, getDijkstraWeight,
            settled, queued, cancellationToken, isMainN);

        return paths.Length < 1 ? (null, double.MaxValue) : paths[0];
    }

    public async Task<(Path? path, double cost)> RunAsync(RoutingNetwork network,
        (SnapPoint sp, bool? direction) source,
        (SnapPoint sp, bool? direction) target,
        DijkstraWeightFunc getDijkstraWeight,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? settled = null,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? queued = null,
        CancellationToken cancellationToken = default,
        IsMainNFunc? isMainN = null)
    {
        var paths = await this.RunAsync(network, source, new[] { target }, getDijkstraWeight, settled, queued, cancellationToken, isMainN);

        return paths.Length < 1 ? (null, double.MaxValue) : paths[0];
    }

    public async Task<(Path? path, double cost)[]> RunAsync(RoutingNetwork network, SnapPoint source,
        IReadOnlyList<SnapPoint> targets,
        DijkstraWeightFunc getDijkstraWeight,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? settled = null,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? queued = null,
        CancellationToken cancellationToken = default,
        IsMainNFunc? isMainN = null)
    {
        var directedTargets = new (SnapPoint sp, bool? direction)[targets.Count];
        for (var i = 0; i < targets.Count; i++)
        {
            directedTargets[i] = (targets[i], null);
        }

        return await this.RunAsync(network, (source, null), directedTargets,
            getDijkstraWeight, settled, queued, cancellationToken, isMainN);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="network"></param>
    /// <param name="source"></param>
    /// <param name="targets"></param>
    /// <param name="getDijkstraWeight"></param>
    /// <param name="settled">This Callback is called for every edge for which the minimal cost is known. If this callback returns false, the edge will not be considered further. (Example usage: building an isochrone, and/or limiting the search to a max cost)</param>
    /// <param name="queued">This callback is called before an edge is loaded. Should not be used to influence route planning (but e.g. to load data when needed)</param>
    /// <param name="isMainN">Optional access-aware predicate. When supplied the search tracks a
    /// sticky <c>leftMain</c> bit and rejects any relaxation back onto a main-N edge after the
    /// first main → non-main transition.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<(Path? path, double cost)[]> RunAsync(RoutingNetwork network,
        (SnapPoint sp, bool? direction) source,
        IReadOnlyList<(SnapPoint sp, bool? direction)> targets,
        DijkstraWeightFunc getDijkstraWeight,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? settled = null,
        Func<(EdgeId edgeId, VertexId vertexId), Task<bool>>? queued = null,
        CancellationToken cancellationToken = default,
        IsMainNFunc? isMainN = null)
    {
        static double GetWorst((uint pointer, double cost)[] targets)
        {
            var worst = 0d;
            for (var i = 0; i < targets.Length; i++)
            {
                if (!(targets[i].cost > worst))
                {
                    continue;
                }

                worst = targets[i].cost;
                if (worst >= double.MaxValue)
                {
                    break;
                }
            }

            return worst;
        }

        var enumerator = network.GetEdgeEnumerator();

        _tree.Clear();
        _visits.Clear();
        _heap.Clear();

        // add sources. leftMain starts false on every source: the search has not yet
        // transitioned out of main-N (or has not yet entered, when source is non-main).
        var sourceForwardVisit = uint.MaxValue;
        if (source.Forward())
        {
            // add forward.
            if (!enumerator.MoveTo(source.sp.EdgeId, true))
            {
                throw new Exception($"Edge in source {source} not found!");
            }

            var sourceFwd = getDijkstraWeight(enumerator, default(PreviousEdgeEnumerable));
            if (sourceFwd.cost > 0)
            {
                // can traverse edge in the forward direction.
                var sourceOffsetCostForward = sourceFwd.cost * (1 - source.sp.OffsetFactor());
                sourceForwardVisit =
                    _tree.AddVisit(enumerator, leftMain: false, localAccess: sourceFwd.localAccess, uint.MaxValue);
                _heap.Push((sourceForwardVisit, enumerator.EdgeId, enumerator.Head, false), sourceOffsetCostForward);
            }
        }

        var sourceBackwardVisit = uint.MaxValue;
        if (source.Backward())
        {
            // add backward.
            if (!enumerator.MoveTo(source.sp.EdgeId, false))
            {
                throw new Exception($"Edge in source {source} not found!");
            }

            var sourceBwd = getDijkstraWeight(enumerator, default(PreviousEdgeEnumerable));
            if (sourceBwd.cost > 0)
            {
                // can traverse edge in the backward direction.
                var sourceOffsetCostBackward = sourceBwd.cost * source.sp.OffsetFactor();
                sourceBackwardVisit =
                    _tree.AddVisit(enumerator, leftMain: false, localAccess: sourceBwd.localAccess, uint.MaxValue);
                _heap.Push((sourceBackwardVisit, enumerator.EdgeId, enumerator.Head, false), sourceOffsetCostBackward);
            }
        }

        // add targets.
        var bestTargets = new (uint pointer, double cost)[targets.Count];
        var targetsPerVertex = new Dictionary<VertexId, List<int>>();
        for (var t = 0; t < targets.Count; t++)
        {
            bestTargets[t] = (uint.MaxValue, double.MaxValue);
            var target = targets[t];

            if (target.Forward())
            {
                // add forward.
                if (!enumerator.MoveTo(target.sp.EdgeId, true))
                {
                    throw new Exception($"Edge in target {target} not found!");
                }

                var targetCostForward = getDijkstraWeight(enumerator, default(PreviousEdgeEnumerable))
                    .cost;
                if (targetCostForward > 0)
                {
                    if (!targetsPerVertex.TryGetValue(enumerator.Tail, out var targetsAtVertex))
                    {
                        targetsAtVertex = new List<int>();
                        targetsPerVertex[enumerator.Tail] = targetsAtVertex;
                    }

                    targetsAtVertex.Add(t);
                }
            }

            if (target.Backward())
            {
                // add backward.
                if (!enumerator.MoveTo(target.sp.EdgeId, false))
                {
                    throw new Exception($"Edge in source {source} not found!");
                }

                var targetCostBackward =
                    getDijkstraWeight(enumerator, default(PreviousEdgeEnumerable)).cost;
                if (targetCostBackward > 0)
                {
                    if (!targetsPerVertex.TryGetValue(enumerator.Tail, out var targetsAtVertex))
                    {
                        targetsAtVertex = new List<int>();
                        targetsPerVertex[enumerator.Tail] = targetsAtVertex;
                    }

                    targetsAtVertex.Add(t);
                }
            }

            // consider paths 'within' a single edge.
            if (source.sp.EdgeId != target.sp.EdgeId)
            {
                continue;
            }

            if (source.sp.Offset == target.sp.Offset)
            {
                // source and target are identical.
                if (sourceForwardVisit != uint.MaxValue &&
                    target.Forward())
                {
                    bestTargets[t] = (sourceForwardVisit, 0);
                }
                else if (sourceBackwardVisit != uint.MaxValue &&
                         target.Backward())
                {
                    bestTargets[t] = (sourceForwardVisit, 0);
                }
            }
            else if (source.sp.Offset < target.sp.Offset &&
                     source.Forward() && target.Forward())
            {
                // the source is earlier in the direction of the edge
                // and the edge can be traversed in this direction.
                if (!enumerator.MoveTo(source.sp.EdgeId, true))
                {
                    throw new Exception($"Edge in source {source} not found!");
                }

                var weight = getDijkstraWeight(enumerator, default(PreviousEdgeEnumerable)).cost *
                             (target.sp.OffsetFactor() - source.sp.OffsetFactor());
                bestTargets[t] = (sourceForwardVisit, weight);
            }
            else if (source.sp.Offset > target.sp.Offset &&
                     source.Backward() && target.Backward())
            {
                // the source is earlier against the direction of the edge
                // and the edge can be traversed in this direction.
                if (!enumerator.MoveTo(source.sp.EdgeId, false))
                {
                    throw new Exception($"Edge in source {source} not found!");
                }

                var weight = getDijkstraWeight(enumerator, default(PreviousEdgeEnumerable)).cost *
                             (source.sp.OffsetFactor() - target.sp.OffsetFactor());
                bestTargets[t] = (sourceBackwardVisit, weight);
            }
        }

        // update worst target cost.
        var worstTargetCost = GetWorst(bestTargets);

        // keep going until heap is empty.
        while (_heap.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // dequeue new visit. Dedup includes leftMain because the same (edge,vertex)
            // is reachable both with leftMain=false and leftMain=true; the former is
            // strictly more permissive but both can occur in the search.
            var currentEntry = _heap.Pop(out var currentCost);
            while (_visits.Contains((currentEntry.edgeId, currentEntry.vertexId, currentEntry.leftMain)))
            {
                // visited before, skip.
                if (_heap.Count == 0)
                {
                    currentEntry = (uint.MaxValue, default, default, false);
                    break;
                }

                currentEntry = _heap.Pop(out currentCost);
            }

            var currentPointer = currentEntry.pointer;
            if (currentPointer == uint.MaxValue)
            {
                break;
            }

            // only call GetVisitWithState after the visited check passes.
            var currentVisit = _tree.GetVisitWithState(currentPointer);

            // log visit.
            if (currentVisit.previousPointer != uint.MaxValue)
            {
                _visits.Add((currentEntry.edgeId, currentEntry.vertexId, currentEntry.leftMain));
            }

            if (settled != null && await settled((currentEntry.edgeId, currentEntry.vertexId)))
            {
                // the best cost to this edge has already been found; current visit can not improve this anymore so we continue
                continue;
            }

            // check if the search needs to stop.
            if (currentCost > worstTargetCost)
            {
                // impossible to improve on cost to any target.
                break;
            }

            // check neighbours.
            if (!enumerator.MoveTo(currentVisit.vertex))
            {
                // no edges, move on!
                continue;
            }

            // check if this is a target.
            if (!targetsPerVertex.TryGetValue(currentVisit.vertex, out var targetsAtVertex))
            {
                targetsAtVertex = null;
            }

            while (enumerator.MoveNext())
            {
                // filter out if u-turns or visits on the same edge.
                var neighbourEdge = enumerator.EdgeId;
                if (neighbourEdge == currentVisit.edge)
                {
                    continue;
                }

                // gets the cost of the current edge.
                var (neighbourCost, turnCost, neighbourLocalAccess) =
                    getDijkstraWeight(enumerator, new PreviousEdgeEnumerable(_tree, currentPointer));
                if (neighbourCost is >= double.MaxValue or <= 0)
                {
                    continue;
                }

                if (turnCost is >= double.MaxValue or < 0)
                {
                    continue;
                }

                // Access-aware state-bit logic. When isMainN is null this block is a no-op:
                // newLeftMain stays false and no relaxation is rejected.
                var newLeftMain = false;
                if (isMainN != null)
                {
                    var prevMain = IsMain(currentVisit.edge, currentVisit.localAccess, currentVisit.leftMain, isMainN);
                    var neighbourMain = IsMain(neighbourEdge, neighbourLocalAccess, currentVisit.leftMain, isMainN);

                    // Rule: once leftMain is true we may not relax onto a main-N edge again.
                    // That would mean re-entering main-N after having departed, i.e. an L-edge
                    // (or non-main-N pocket) used as through-traffic between two main-N segments.
                    if (currentVisit.leftMain && neighbourMain) continue;

                    // newLeftMain is sticky and flips on the first main-N → non-main-N transition.
                    newLeftMain = currentVisit.leftMain || (prevMain && !neighbourMain);
                }

                // if the vertex has targets, check if this edge is a match.
                var neighbourPointer = uint.MaxValue;
                if (targetsAtVertex != null)
                {
                    // only consider targets when found for the 'from' vertex.
                    // and when this in not a u-turn.
                    foreach (var t in targetsAtVertex)
                    {
                        var target = targets[t];
                        if (target.sp.EdgeId != neighbourEdge)
                        {
                            continue;
                        }

                        // check directions.
                        if (enumerator.Forward && !target.Forward())
                        {
                            continue;
                        }

                        if (!enumerator.Forward && !target.Backward())
                        {
                            continue;
                        }

                        // there is a target on this edge, calculate the cost.
                        // calculate the cost from the 'from' vertex to the target.
                        var targetCost = enumerator.Forward
                            ? neighbourCost * target.sp.OffsetFactor()
                            : neighbourCost * (1 - target.sp.OffsetFactor());
                        // this is the case where the target is on this edge
                        // and there is a path to 'from' before.
                        targetCost += currentCost;

                        targetCost += turnCost;

                        // if this is an improvement, use it!
                        var targetBestCost = bestTargets[t].cost;
                        if (!(targetCost < targetBestCost))
                        {
                            continue;
                        }

                        // this is an improvement.
                        neighbourPointer = _tree.AddVisit(enumerator, newLeftMain, neighbourLocalAccess, currentPointer);
                        bestTargets[t] = (neighbourPointer, targetCost);

                        // update worst.
                        worstTargetCost = GetWorst(bestTargets);
                    }
                }

                if (queued != null &&
                    await queued((enumerator.EdgeId, enumerator.Head)))
                {
                    // don't queue this edge if the queued function returns true.
                    continue;
                }

                // add visit if not added yet.
                if (neighbourPointer == uint.MaxValue)
                {
                    neighbourPointer =
                        _tree.AddVisit(enumerator, newLeftMain, neighbourLocalAccess, currentPointer);
                }

                // add visit to heap.
                _heap.Push((neighbourPointer, enumerator.EdgeId, enumerator.Head, newLeftMain), neighbourCost + currentCost + turnCost);
            }
        }

        var paths = new (Path? path, double cost)[targets.Count];
        for (var p = 0; p < paths.Length; p++)
        {
            var bestTarget = bestTargets[p];
            if (bestTarget.pointer == uint.MaxValue)
            {
                paths[p] = (null, double.MaxValue);
                continue;
            }

            // build resulting path.
            var path = new Path(network);
            var visit = _tree.GetVisit(bestTarget.pointer);

            // path is at least two edges.
            while (true)
            {
                if (visit.previousPointer == uint.MaxValue)
                {
                    enumerator.MoveTo(visit.edge);
                    path.Prepend(visit.edge, visit.forward);
                    break;
                }

                path.Prepend(visit.edge, visit.forward);
                visit = _tree.GetVisit(visit.previousPointer);
            }

            // add the offsets.
            var target = targets[p];
            path.Offset1 = path.First.direction ? source.sp.Offset : (ushort)(ushort.MaxValue - source.sp.Offset);
            path.Offset2 = path.Last.direction
                ? target.sp.Offset
                : (ushort)(ushort.MaxValue - target.sp.Offset);

            paths[p] = (path, bestTarget.cost);
        }

        return paths;
    }

    /// <summary>
    /// Hybrid "is main-N?" predicate. Consults <paramref name="isMainN"/> first; when that
    /// returns null (the tile isn't classified yet) falls back to <c>!leftMain</c>. The fallback
    /// is path-dependent: while we still believe we are in main (<c>leftMain == false</c>) an
    /// unclassified edge is treated as main, so no spurious leftMain transition fires; once we
    /// have left main, an unclassified edge is treated as non-main, so no spurious re-entry
    /// rejection fires.
    /// </summary>
    private static bool IsMain(EdgeId edgeId, bool localAccess, bool leftMain, IsMainNFunc isMainN)
    {
        var verdict = isMainN(edgeId, localAccess);
        return verdict ?? !leftMain;
    }

    /// <summary>
    /// Gets a fresh Dijkstra instance for this call. Was previously a [ThreadStatic]
    /// reuse, but that's unsafe under async/await + ThreadPool reuse: thread A starts
    /// a routing call on instance D, awaits a callback, returns to the pool, picks up
    /// a second routing call — Default returns the same D, and now two routing calls
    /// mutate D's _heap/_visits/_tree concurrently → "concurrent update" crash.
    /// Allocating three small collections per call is cheap vs. the routing work.
    /// </summary>
    public static Dijkstra Default => new();
}
