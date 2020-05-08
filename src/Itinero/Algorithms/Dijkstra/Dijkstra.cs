using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Itinero.Algorithms.DataStructures;
using Itinero.Data.Graphs;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]
namespace Itinero.Algorithms.Dijkstra
{
    /// <summary>
    /// A dijkstra implementation.
    /// </summary>
    internal class Dijkstra
    {
        private readonly PathTree _tree = new PathTree();
        private readonly HashSet<VertexId> _visits = new HashSet<VertexId>();
        private readonly BinaryHeap<uint> _heap = new BinaryHeap<uint>();

        /// <summary>
        /// Calculates one path between a single source and target.
        /// </summary>
        /// <returns>The path.</returns>
        public Path? Run(RouterDb routerDb, SnapPoint source, SnapPoint target,
            Func<RouterDbEdgeEnumerator, uint> getWeight, Func<VertexId, bool>? settled = null,
            Func<VertexId, bool>? queued = null)
        {
            var paths = Run(routerDb, source, new[] {target}, getWeight, settled, queued);
            if (paths == null) return null;
            if (paths.Length < 1) return null;

            return paths[0];
        }

        /// <summary>
        /// Calculates one path between a single source and target.
        /// </summary>
        /// <returns>The path.</returns>
        public Path? Run2(RouterDb routerDb, SnapPoint source, SnapPoint target, 
            Func<RouterDbEdgeEnumerator, uint> getWeight, Func<VertexId, bool>? settled = null, Func<VertexId, bool>? queued = null)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            _tree.Clear();
            _visits.Clear();
            _heap.Clear();

            // add sources.
            // add forward.
            if (!enumerator.MoveToEdge(source.EdgeId, true)) throw new Exception($"Edge in source {source} not found!");
            var sourceCostForward = getWeight(enumerator);
            if (sourceCostForward > 0)
            {
                // can traverse edge in the forward direction.
                var sourceOffsetCostForward = sourceCostForward * (1 - source.OffsetFactor());
                var p = _tree.AddVisit(enumerator.To, source.EdgeId, uint.MaxValue);
                _heap.Push(p, sourceOffsetCostForward);
            }

            // add backward.
            if (!enumerator.MoveToEdge(source.EdgeId, false))
                throw new Exception($"Edge in source {source} not found!");
            var sourceCostBackward = getWeight(enumerator);
            if (sourceCostBackward > 0)
            {
                // can traverse edge in the backward direction.
                var sourceOffsetCostBackward = sourceCostBackward * source.OffsetFactor();
                var p = _tree.AddVisit(enumerator.To, source.EdgeId, uint.MaxValue);
                _heap.Push(p, sourceOffsetCostBackward);
            }

            // add targets.
            (uint pointer, double cost, bool forward, SnapPoint target) bestTarget = (uint.MaxValue, double.MaxValue,
                false,
                default);
            var targetMaxCost = 0d;
            var targetsPerVertex = new Dictionary<VertexId, (double cost, bool forward, SnapPoint target)>();
            // add forward.
            if (!enumerator.MoveToEdge(target.EdgeId, true)) throw new Exception($"Edge in target {target} not found!");
            var targetCostForward = getWeight(enumerator);
            if (targetCostForward > 0)
            {
                // can traverse edge in the forward direction, we can the from vertex.
                var targetCostForwardOffset = targetCostForward * target.OffsetFactor();
                targetsPerVertex[enumerator.From] = (targetCostForwardOffset, true, target);
                if (targetCostForwardOffset > targetMaxCost)
                {
                    targetMaxCost = targetCostForwardOffset;
                }
            }

            // add backward.
            if (!enumerator.MoveToEdge(target.EdgeId, false))
                throw new Exception($"Edge in target {target} not found!");
            var targetCostBackward = getWeight(enumerator);
            if (targetCostBackward > 0)
            {
                // can traverse edge in the forward direction, we can the from vertex.
                var targetCostBackwardOffset = targetCostBackward * target.OffsetFactor();
                targetsPerVertex[enumerator.From] = (targetCostBackwardOffset, false, target);
                if (targetCostBackwardOffset > targetMaxCost)
                {
                    targetMaxCost = targetCostBackwardOffset;
                }
            }

            // keep going until heap is empty.
            while (_heap.Count > 0)
            {
                if (_visits.Count > (1 << 20))
                {
                    // TODO: come up with a stop condition that makes more sense to prevent the global network being loaded
                    // when a route is not found.
                    break;
                }

                // dequeue new visit.
                var currentPointer = _heap.Pop(out var currentCost);
                var currentVisit = _tree.GetVisit(currentPointer);
                while (_visits.Contains(currentVisit.vertex))
                {
                    // visited before, skip.
                    currentPointer = uint.MaxValue;
                    if (_heap.Count == 0)
                    {
                        break;
                    }

                    currentPointer = _heap.Pop(out currentCost);
                    currentVisit = _tree.GetVisit(currentPointer);
                }

                if (currentPointer == uint.MaxValue)
                {
                    break;
                }

                // log visit.
                _visits.Add(currentVisit.vertex);

                if (settled != null && settled(currentVisit.vertex))
                {
                    // break if requested.
                    continue;
                }

                // check if the search needs to stop.
                if (currentCost + targetMaxCost > bestTarget.cost)
                {
                    // impossible to improve on cost to any target.
                    break;
                }

                // check if this is a target.
                if (targetsPerVertex.TryGetValue(currentVisit.vertex, out var targetDetails))
                {
                    // this vertex is a target, check for an improvement.
                    var targetCost = targetDetails.cost;
                    targetCost += currentCost;
                    if (targetCost < bestTarget.cost)
                    {
                        bestTarget = (currentPointer, targetCost, targetDetails.forward, targetDetails.target);
                    }

                    targetsPerVertex.Remove(currentVisit.vertex);
                }

                // check neighbours.
                if (!enumerator.MoveTo(currentVisit.vertex))
                {
                    // no edges, move on!
                    continue;
                }

                while (enumerator.MoveNext())
                {
                    var neighbourCost = getWeight(enumerator);
                    if (neighbourCost >= double.MaxValue ||
                        neighbourCost <= 0) continue;

                    var neighbourEdge = enumerator.Id;
                    if (neighbourEdge == currentVisit.edge) continue; // don't consider u-turns.

                    if (queued != null &&
                        queued.Invoke(enumerator.To))
                    { // don't queue this vertex if the queued function returns true.
                        continue;
                    }

                    var neighbourPointer = _tree.AddVisit(enumerator.To, enumerator.Id, currentPointer);
                    _heap.Push(neighbourPointer, neighbourCost + currentCost);
                }
            }

            if (bestTarget.pointer == uint.MaxValue) return null;

            // build resulting path.
            var path = new Path(routerDb.Network);
            var visit = _tree.GetVisit(bestTarget.pointer);

            if (!enumerator.MoveToEdge(bestTarget.target.EdgeId, bestTarget.forward))
                throw new Exception($"Edge in bestTarget {bestTarget} not found!");
            if (bestTarget.target.EdgeId == source.EdgeId)
            {
                // path is just inside one edge.
                path.Prepend(bestTarget.target.EdgeId, enumerator.To);
            }
            else
            {
                // path is at least two edges.
                path.Prepend(bestTarget.target.EdgeId, enumerator.To);
                while (true)
                {
                    if (visit.previousPointer == uint.MaxValue)
                    {
                        enumerator.MoveToEdge(visit.edge);
                        path.Prepend(visit.edge, visit.vertex);
                        break;
                    }

                    path.Prepend(visit.edge, visit.vertex);
                    visit = _tree.GetVisit(visit.previousPointer);
                }
            }

            // add the offsets.
            path.Offset1 = path[0].forward ? source.Offset : (ushort) (ushort.MaxValue - source.Offset);
            path.Offset2 = path[path.Count - 1].forward
                ? bestTarget.target.Offset
                : (ushort) (ushort.MaxValue - bestTarget.target.Offset);

            return path;
        }

        /// <summary>
        /// Calculates all paths from a single source to many targets.
        /// </summary>
        /// <returns>The path.</returns>
        public Path[] Run(RouterDb routerDb, SnapPoint source, IReadOnlyList<SnapPoint> targets,
            Func<RouterDbEdgeEnumerator, uint> getWeight, Func<VertexId, bool>? settled = null, Func<VertexId, bool>? queued = null)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            _tree.Clear();
            _visits.Clear();
            _heap.Clear();

            // add sources.
            // add forward.
            if (!enumerator.MoveToEdge(source.EdgeId, true)) throw new Exception($"Edge in source {source} not found!");
            var sourceCostForward = getWeight(enumerator);
            if (sourceCostForward > 0)
            {
                // can traverse edge in the forward direction.
                var sourceOffsetCostForward = sourceCostForward * (1 - source.OffsetFactor());
                var p = _tree.AddVisit(enumerator.To, source.EdgeId, uint.MaxValue);
                _heap.Push(p, sourceOffsetCostForward);
            }

            // add backward.
            if (!enumerator.MoveToEdge(source.EdgeId, false))
                throw new Exception($"Edge in source {source} not found!");
            var sourceCostBackward = getWeight(enumerator);
            if (sourceCostBackward > 0)
            {
                // can traverse edge in the backward direction.
                var sourceOffsetCostBackward = sourceCostBackward * source.OffsetFactor();
                var p = _tree.AddVisit(enumerator.To, source.EdgeId, uint.MaxValue);
                _heap.Push(p, sourceOffsetCostBackward);
            }

            // add targets.
            // TODO: cater to the default first, one target per vertex.
            var worstTargetCost = double.MaxValue;
            var bestTargets = new (uint pointer, double cost, bool forward, SnapPoint target)[targets.Count];
            var targetMaxCost = 0d;
            var targetsPerVertex = new Dictionary<VertexId, List<(int t, double cost, bool forward, SnapPoint target)>>();
            for (var t = 0; t < targets.Count; t++)
            {
                bestTargets[t] = (uint.MaxValue, double.MaxValue, false, default);
                var target = targets[t];
                
                // add forward.
                if (!enumerator.MoveToEdge(target.EdgeId, true)) throw new Exception($"Edge in target {target} not found!");
                var targetCostForward = getWeight(enumerator);
                if (targetCostForward > 0)
                {
                    // can traverse edge in the forward direction, we can the from vertex.
                    var targetCostForwardOffset = targetCostForward * target.OffsetFactor();
                    if (!targetsPerVertex.TryGetValue(enumerator.From, out var targetsAtVertex))
                    {
                        targetsAtVertex = new List<(int t, double cost, bool forward, SnapPoint target)>();
                        targetsPerVertex[enumerator.From] = targetsAtVertex;
                    }
                    targetsAtVertex.Add((t, targetCostForwardOffset, false, target));
                    if (targetCostForwardOffset > targetMaxCost)
                    {
                        targetMaxCost = targetCostForwardOffset;
                    }
                }

                // add backward.
                if (!enumerator.MoveToEdge(target.EdgeId, true))
                    throw new Exception($"Edge in target {target} not found!");
                var targetCostBackward = getWeight(enumerator);
                if (targetCostBackward > 0)
                {
                    // can traverse edge in the forward direction, we can the from vertex.
                    var targetCostBackwardOffset = targetCostBackward * target.OffsetFactor();
                    if (!targetsPerVertex.TryGetValue(enumerator.From, out var targetsAtVertex))
                    {
                        targetsAtVertex = new List<(int t, double cost, bool forward, SnapPoint target)>();
                        targetsPerVertex[enumerator.From] = targetsAtVertex;
                    }
                    targetsAtVertex.Add((t, targetCostBackwardOffset, false, target));
                    if (targetCostBackwardOffset > targetMaxCost)
                    {
                        targetMaxCost = targetCostBackwardOffset;
                    }
                }
            }

            // keep going until heap is empty.
            while (_heap.Count > 0)
            {
                if (_visits.Count > (1 << 20))
                {
                    // TODO: come up with a stop condition that makes more sense to prevent the global network being loaded
                    // when a route is not found.
                    break;
                }

                // dequeue new visit.
                var currentPointer = _heap.Pop(out var currentCost);
                var currentVisit = _tree.GetVisit(currentPointer);
                while (_visits.Contains(currentVisit.vertex))
                {
                    // visited before, skip.
                    currentPointer = uint.MaxValue;
                    if (_heap.Count == 0)
                    {
                        break;
                    }

                    currentPointer = _heap.Pop(out currentCost);
                    currentVisit = _tree.GetVisit(currentPointer);
                }

                if (currentPointer == uint.MaxValue)
                {
                    break;
                }

                // log visit.
                _visits.Add(currentVisit.vertex);

                if (settled != null && settled(currentVisit.vertex))
                {
                    // break if requested.
                    break;
                }
                
                // check if the search needs to stop.
                if (currentCost + targetMaxCost > worstTargetCost)
                {
                    // impossible to improve on cost to any target.
                    break;
                }

                // check if this is a target.
                if (targetsPerVertex.TryGetValue(currentVisit.vertex, out var targetDetails))
                {
                    var t = 0;
                    while (t < targetDetails.Count)
                    {
                        var targetDetail = targetDetails[t];
                        var targetCost = targetDetail.cost;
                        targetCost += currentCost;
                        if (targetCost < bestTargets[targetDetail.t].cost)
                        {
                            bestTargets[targetDetail.t] = (currentPointer, targetCost, targetDetail.forward, targetDetail.target);
                        }

                        t++;
                    }

                    var worst = 0d;
                    for (t = 0; t < bestTargets.Length; t++)
                    {
                        if (!(bestTargets[t].cost > worst)) continue;
                        
                        worst = bestTargets[t].cost;
                        if (worst >= double.MaxValue) break;
                    }
                    worstTargetCost = worst;

                    targetsPerVertex.Remove(currentVisit.vertex);
                }

                // check neighbours.
                if (!enumerator.MoveTo(currentVisit.vertex))
                {
                    // no edges, move on!
                    continue;
                }

                while (enumerator.MoveNext())
                {
                    var neighbourCost = getWeight(enumerator);
                    if (neighbourCost >= double.MaxValue ||
                        neighbourCost <= 0) continue;

                    var neighbourEdge = enumerator.Id;
                    if (neighbourEdge == currentVisit.edge) continue; // don't consider u-turns.

                    if (queued != null &&
                        queued.Invoke(enumerator.To))
                    { // don't queue this vertex if the queued function returns true.
                        continue;
                    }

                    var neighbourPointer = _tree.AddVisit(enumerator.To, enumerator.Id, currentPointer);
                    _heap.Push(neighbourPointer, neighbourCost + currentCost);
                }
            }

            var paths = new Path[targets.Count];
            for (var p = 0; p < paths.Length; p++)
            {
                var bestTarget = bestTargets[p];
                if (bestTarget.pointer == uint.MaxValue) continue;

                // build resulting path.
                var path = new Path(routerDb.Network);
                var visit = _tree.GetVisit(bestTarget.pointer);

                if (!enumerator.MoveToEdge(bestTarget.target.EdgeId, bestTarget.forward))
                    throw new Exception($"Edge in bestTarget {bestTarget} not found!");
                if (bestTarget.target.EdgeId == source.EdgeId)
                {
                    // path is just inside one edge.
                    path.Prepend(bestTarget.target.EdgeId, enumerator.To);
                }
                else
                {
                    // path is at least two edges.
                    path.Prepend(bestTarget.target.EdgeId, enumerator.To);
                    while (true)
                    {
                        if (visit.previousPointer == uint.MaxValue)
                        {
                            enumerator.MoveToEdge(visit.edge);
                            path.Prepend(visit.edge, visit.vertex);
                            break;
                        }

                        path.Prepend(visit.edge, visit.vertex);
                        visit = _tree.GetVisit(visit.previousPointer);
                    }
                }

                // add the offsets.
                path.Offset1 = path[0].forward ? source.Offset : (ushort) (ushort.MaxValue - source.Offset);
                path.Offset2 = path[path.Count - 1].forward
                    ? bestTarget.target.Offset
                    : (ushort) (ushort.MaxValue - bestTarget.target.Offset);

                paths[p] = path;
            }

            return paths;
        }

        private static readonly ThreadLocal<Dijkstra> DefaultLazy = new ThreadLocal<Dijkstra>(() => new Dijkstra());
        
        /// <summary>
        /// Gets the default dijkstra instance (for the local thread).
        /// </summary>
        public static Dijkstra Default => DefaultLazy.Value;
    }
}