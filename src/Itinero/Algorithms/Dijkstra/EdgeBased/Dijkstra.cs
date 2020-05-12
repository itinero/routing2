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
namespace Itinero.Algorithms.Dijkstra.EdgeBased
{
    /// <summary>
    /// An edge-based dijkstra implementation.
    /// </summary>
    internal class Dijkstra
    {
        private readonly PathTree _tree = new PathTree();
        private readonly HashSet<(EdgeId edgeId, VertexId vertexId)> _visits = new HashSet<(EdgeId edgeId, VertexId vertexId)>();
        private readonly BinaryHeap<uint> _heap = new BinaryHeap<uint>();


        /// <summary>
        /// Calculates a path.
        /// </summary>
        /// <returns>The path.</returns>
        public Path? Run(RouterDb routerDb, (SnapPoint sp, bool? direction) source,
            (SnapPoint sp, bool? direction) target,
            Func<RouterDbEdgeEnumerator, double> getWeight,
            Func<(EdgeId edgeId, VertexId vertexId), bool>? settled = null,
            Func<(EdgeId edgeId, VertexId vertexId), bool>? queued = null)
        {
            var paths = Run(routerDb, source, new[] {target}, getWeight, settled, queued);
            if (paths == null) return null;
            if (paths.Length < 1) return null;

            return paths[0];
        }
        
        /// <summary>
        /// Calculates a path.
        /// </summary>
        /// <returns>The path.</returns>
        public Path? Run2(RouterDb routerDb, (SnapPoint sp, bool? direction) source,
            (SnapPoint sp, bool? direction) target,
            Func<RouterDbEdgeEnumerator, double> getWeight,
            Func<(EdgeId edgeId, VertexId vertexId), bool>? settled = null,
            Func<(EdgeId edgeId, VertexId vertexId), bool>? queued = null)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            var path = new Path(routerDb.Network);

            if (source.sp.EdgeId == target.sp.EdgeId)
            {
                if (!enumerator.MoveToEdge(source.sp.EdgeId))
                    throw new Exception($"Edge in source {source} not found!");
                if (source.sp.Offset < target.sp.Offset &&
                    source.Forward() && target.Forward())
                {
                    path.Prepend(source.sp.EdgeId, enumerator.To);
                    path.Offset1 = source.sp.Offset;
                    path.Offset2 = target.sp.Offset;

                    return path;
                }
                else if (source.Backward() && target.Backward())
                {
                    path.Prepend(source.sp.EdgeId, enumerator.From);
                    path.Offset1 = (ushort) (ushort.MaxValue - source.sp.Offset);
                    path.Offset2 = (ushort) (ushort.MaxValue - target.sp.Offset);

                    return path;
                }
            }

            // ok we need to calculate things, clear things first.
            _tree.Clear();
            _visits.Clear();
            _heap.Clear();

            // add sources.
            if (source.Forward())
            {
                // add forward.
                if (!enumerator.MoveToEdge(source.sp.EdgeId, true))
                    throw new Exception($"Edge in source {source} not found!");
                var sourceCostForward = getWeight(enumerator);
                if (sourceCostForward > 0)
                {
                    // can traverse edge in the forward direction.
                    var sourceOffsetCostForward = sourceCostForward * (1 - source.sp.OffsetFactor());
                    var p = _tree.AddVisit(enumerator.To, source.sp.EdgeId, uint.MaxValue);
                    _heap.Push(p, sourceOffsetCostForward);
                }
            }

            if (source.Backward())
            {
                // add backward.
                if (!enumerator.MoveToEdge(source.sp.EdgeId, false))
                    throw new Exception($"Edge in source {source} not found!");
                var sourceCostBackward = getWeight(enumerator);
                if (sourceCostBackward > 0)
                {
                    // can traverse edge in the backward direction.
                    var sourceOffsetCostBackward = sourceCostBackward * source.sp.OffsetFactor();
                    var p = _tree.AddVisit(enumerator.To, source.sp.EdgeId, uint.MaxValue);
                    _heap.Push(p, sourceOffsetCostBackward);
                }
            }

            // add targets.
            (uint pointer, double cost, bool forward, SnapPoint target) bestTarget = (uint.MaxValue, double.MaxValue,
                false,
                default);
            var targetMaxCost = 0d;
            (double costForward, double costBackward) targetOnEdge = (double.MaxValue, double.MaxValue);

            if (target.Forward())
            {
                // add forward.
                if (!enumerator.MoveToEdge(target.sp.EdgeId, true))
                    throw new Exception($"Edge in target {target} not found!");
                var targetCostForward = getWeight(enumerator);
                if (targetCostForward > 0)
                {
                    // can traverse edge in the forward direction, we can the from vertex.
                    targetCostForward *= target.sp.OffsetFactor();
                    if (targetCostForward > targetMaxCost)
                    {
                        targetMaxCost = targetCostForward;
                    }

                    targetOnEdge = (targetCostForward, targetOnEdge.costBackward);
                }
            }

            if (target.Backward())
            {
                // add backward.
                if (!enumerator.MoveToEdge(target.sp.EdgeId, false))
                    throw new Exception($"Edge in target {target} not found!");
                var targetCostBackward = getWeight(enumerator);
                if (targetCostBackward > 0)
                {
                    // can traverse edge in the forward direction, we can the from vertex.
                    targetCostBackward *= target.sp.OffsetFactor();
                    if (targetCostBackward > targetMaxCost)
                    {
                        targetMaxCost = targetCostBackward;
                    }

                    targetOnEdge = (targetOnEdge.costForward, targetCostBackward);
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
                while (_visits.Contains((currentVisit.edge, currentVisit.vertex)))
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
                if (currentVisit.previousPointer != uint.MaxValue)
                {
                    _visits.Add((currentVisit.edge, currentVisit.vertex));
                }

                if (settled != null && settled((currentVisit.edge, currentVisit.vertex)))
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
                if (currentVisit.edge == target.sp.EdgeId &&
                    currentVisit.previousPointer != uint.MaxValue)
                {
                    enumerator.MoveToEdge(currentVisit.edge);
                    var forward = enumerator.To == currentVisit.vertex;

                    var targetCost = targetOnEdge.costForward;
                    if (!forward) targetCost = targetOnEdge.costBackward;

                    // this vertex is a target, check for an improvement.
                    targetCost += currentCost;
                    targetCost -= getWeight(enumerator);
                    if (targetCost < bestTarget.cost)
                    {
                        bestTarget = (currentPointer, targetCost, forward, target.sp);
                    }
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
                        queued.Invoke((enumerator.Id, enumerator.To)))
                    {
                        // don't queue this vertex if the queued function returns true.
                        continue;
                    }

                    var neighbourPointer = _tree.AddVisit(enumerator.To, enumerator.Id, currentPointer);
                    _heap.Push(neighbourPointer, neighbourCost + currentCost);
                }
            }

            if (bestTarget.pointer == uint.MaxValue) return null;

            // build resulting path.
            var visit = _tree.GetVisit(bestTarget.pointer);

            if (!enumerator.MoveToEdge(bestTarget.target.EdgeId, bestTarget.forward))
                throw new Exception($"Edge in bestTarget {bestTarget} not found!");
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

            // add the offsets.
            path.Offset1 = path.First.direction ? source.sp.Offset : (ushort) (ushort.MaxValue - source.sp.Offset);
            path.Offset2 = path.Last.direction
                ? bestTarget.target.Offset
                : (ushort) (ushort.MaxValue - bestTarget.target.Offset);

            return path;
        }

        /// <summary>
        /// Calculates a path.
        /// </summary>
        /// <returns>The path.</returns>
        public Path[] Run(RouterDb routerDb, (SnapPoint sp, bool? direction) source,
            IReadOnlyList<(SnapPoint sp, bool? direction)> targets,
            Func<RouterDbEdgeEnumerator, double> getWeight,
            Func<(EdgeId edgeId, VertexId vertexId), bool>? settled = null,
            Func<(EdgeId edgeId, VertexId vertexId), bool>? queued = null)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            var paths = new Path[targets.Count];
            var allPathsFound = true;
            for (var t = 0; t < targets.Count; t++)
            {
                var target = targets[t];
                var path = new Path(routerDb.Network);

                if (source.sp.EdgeId != target.sp.EdgeId)
                {
                    allPathsFound = false;
                    continue;
                }
                
                if (!enumerator.MoveToEdge(source.sp.EdgeId))
                    throw new Exception($"Edge in source {source} not found!");
                if (source.sp.Offset < target.sp.Offset &&
                    source.Forward() && target.Forward())
                {
                    path.Prepend(source.sp.EdgeId, enumerator.To);
                    path.Offset1 = source.sp.Offset;
                    path.Offset2 = target.sp.Offset;

                    paths[t] = path;
                }
                else if (source.Backward() && target.Backward())
                {
                    path.Prepend(source.sp.EdgeId, enumerator.From);
                    path.Offset1 = (ushort) (ushort.MaxValue - source.sp.Offset);
                    path.Offset2 = (ushort) (ushort.MaxValue - target.sp.Offset);

                    paths[t] = path;
                }
                else
                {
                    allPathsFound = false;
                }
            }

            if (allPathsFound)
            {
                return paths;
            }

            // ok we need to calculate things, clear things first.
            _tree.Clear();
            _visits.Clear();
            _heap.Clear();

            // add sources.
            if (source.Forward())
            {
                // add forward.
                if (!enumerator.MoveToEdge(source.sp.EdgeId, true))
                    throw new Exception($"Edge in source {source} not found!");
                var sourceCostForward = getWeight(enumerator);
                if (sourceCostForward > 0)
                {
                    // can traverse edge in the forward direction.
                    var sourceOffsetCostForward = sourceCostForward * (1 - source.sp.OffsetFactor());
                    var p = _tree.AddVisit(enumerator.To, source.sp.EdgeId, uint.MaxValue);
                    _heap.Push(p, sourceOffsetCostForward);
                }
            }

            if (source.Backward())
            {
                // add backward.
                if (!enumerator.MoveToEdge(source.sp.EdgeId, false))
                    throw new Exception($"Edge in source {source} not found!");
                var sourceCostBackward = getWeight(enumerator);
                if (sourceCostBackward > 0)
                {
                    // can traverse edge in the backward direction.
                    var sourceOffsetCostBackward = sourceCostBackward * source.sp.OffsetFactor();
                    var p = _tree.AddVisit(enumerator.To, source.sp.EdgeId, uint.MaxValue);
                    _heap.Push(p, sourceOffsetCostBackward);
                }
            }

            // add targets.
            var bestTargets = new (uint pointer, double cost, bool forward, SnapPoint target)[targets.Count];
            var worstTargetCost = double.MaxValue;
            var targetMaxCost = 0d;
            var targetsOnEdges = new Dictionary<EdgeId, List<(int t, double costForward, double costBackward)>>();

            for (var t = 0; t < targets.Count; t++)
            {
                var target = targets[t];

                // take into account existing paths.
                var path = paths[t];
                if (path != null)
                {
                    // paths have max one edge.
                    bestTargets[t] = (uint.MaxValue, path.Weight(e =>
                    {
                        enumerator.MoveToEdge(e.edge, e.direction);
                        return getWeight(enumerator);
                    }), path.First.direction, target.sp);
                }
                else
                {
                    bestTargets[t] = (uint.MaxValue, double.MaxValue, false, target.sp);
                }

                var targetCostForward = double.MaxValue;
                var targetCostBackward = double.MaxValue;
                
                if (target.Forward())
                {
                    // add forward.
                    if (!enumerator.MoveToEdge(target.sp.EdgeId, true))
                        throw new Exception($"Edge in target {target} not found!");
                    targetCostForward = getWeight(enumerator);
                }
                if (target.Backward())
                {
                    // add backward.
                    if (!enumerator.MoveToEdge(target.sp.EdgeId, false))
                        throw new Exception($"Edge in target {target} not found!");
                    targetCostBackward = getWeight(enumerator);
                }

                if (targetCostForward < double.MaxValue || targetCostBackward < double.MaxValue)
                {
                    // can traverse edge in the forward direction, we can the from vertex.
                    targetCostForward *= target.sp.OffsetFactor();
                    if (targetCostForward > targetMaxCost)
                    {
                        targetMaxCost = targetCostForward;
                    }

                    if (!targetsOnEdges.TryGetValue(target.sp.EdgeId, out var targetsOnEdge))
                    {
                        targetsOnEdge = new List<(int t, double costForward, double costBackward)>();
                        targetsOnEdges.Add(target.sp.EdgeId, targetsOnEdge);
                    }

                    targetsOnEdge.Add((t, targetCostForward, targetCostBackward));
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
                while (_visits.Contains((currentVisit.edge, currentVisit.vertex)))
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
                if (currentVisit.previousPointer != uint.MaxValue)
                {
                    _visits.Add((currentVisit.edge, currentVisit.vertex));
                }

                if (settled != null && settled((currentVisit.edge, currentVisit.vertex)))
                {
                    // break if requested.
                    continue;
                }
                
                // check if the search needs to stop.
                if (currentCost + targetMaxCost > worstTargetCost)
                {
                    // impossible to improve on cost to any target.
                    break;
                }

                // check if this is a target.
                if (currentVisit.previousPointer != uint.MaxValue &&
                    targetsOnEdges.TryGetValue(currentVisit.edge, out var targetsOnEdge))
                {
                    // this edge has a target.
                    enumerator.MoveToEdge(currentVisit.edge);
                    var forward = enumerator.To == currentVisit.vertex;
                    
                    var i = 0;
                    while (i < targetsOnEdge.Count)
                    {
                        var targetOnEdge = targetsOnEdge[i];
                        
                        // get the target cost.
                        var targetCost = targetOnEdge.costForward;
                        if (!forward) targetCost = targetOnEdge.costBackward;
                        
                        // this edge has a target, the target on the edge has been reached.
                        // check for an improvement.
                        targetCost += currentCost;
                        targetCost -= getWeight(enumerator);
                        var currentTarget = bestTargets[targetOnEdge.t];
                        if (targetCost < currentTarget.cost)
                        {
                            bestTargets[i] = (currentPointer, targetCost, forward, currentTarget.target);
                        }
                        
                        // remove target cost if both are infinite.
                        if ((forward && targetOnEdge.costBackward >= double.MaxValue) ||
                            (!forward && targetOnEdge.costForward >= double.MaxValue))
                        {
                            targetsOnEdge.RemoveAt(i);
                        }
                        else
                        {
                            targetsOnEdge[i] = (targetOnEdge.t,
                                forward ? double.MaxValue : targetOnEdge.costForward,
                                !forward ? double.MaxValue : targetOnEdge.costBackward);
                            i++;
                        }
                    }

                    // update worst.
                    var worst = 0d;
                    for (i = 0; i < bestTargets.Length; i++)
                    {
                        if (!(bestTargets[i].cost > worst)) continue;
                        
                        worst = bestTargets[i].cost;
                        if (worst >= double.MaxValue) break;
                    }
                    worstTargetCost = worst;

                    // remove target if there is no more to search.
                    if (targetsOnEdge.Count == 0)
                    {
                        targetsOnEdges.Remove(currentVisit.edge);
                        if (targetsOnEdges.Count == 0) break;
                    }
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
                        queued.Invoke((enumerator.Id, enumerator.To)))
                    {
                        // don't queue this vertex if the queued function returns true.
                        continue;
                    }

                    var neighbourPointer = _tree.AddVisit(enumerator.To, enumerator.Id, currentPointer);
                    _heap.Push(neighbourPointer, neighbourCost + currentCost);
                }
            }

            for (var t = 0; t < targets.Count; t++)
            {
                var bestTarget = bestTargets[t];
                if (bestTarget.pointer == uint.MaxValue) continue;

                // build resulting paths.
                var path = new Path(routerDb.Network);
                var visit = _tree.GetVisit(bestTarget.pointer);

                if (!enumerator.MoveToEdge(bestTarget.target.EdgeId, bestTarget.forward))
                    throw new Exception($"Edge in bestTarget {bestTarget} not found!");
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

                // add the offsets.
                path.Offset1 = path.First.direction ? source.sp.Offset : (ushort) (ushort.MaxValue - source.sp.Offset);
                path.Offset2 = path.Last.direction
                    ? bestTarget.target.Offset
                    : (ushort) (ushort.MaxValue - bestTarget.target.Offset);

                paths[t] = path;
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