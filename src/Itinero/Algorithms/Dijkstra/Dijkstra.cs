using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Itinero.Algorithms.DataStructures;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.TurnCosts;

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
        
        public Path? Run(Network network, SnapPoint source, SnapPoint target,
            DijkstraWeightFunc getDijkstraWeight, Func<VertexId, bool>? settled = null,
            Func<VertexId, bool>? queued = null)
        {
            var paths = Run(network, source, new[] {target}, getDijkstraWeight, settled, queued);
            if (paths == null) return null;
            if (paths.Length < 1) return null;

            return paths[0];
        }

        public Path[] Run(Network network, SnapPoint source, IReadOnlyList<SnapPoint> targets,
            DijkstraWeightFunc getDijkstraWeight, Func<VertexId, bool>? settled = null, Func<VertexId, bool>? queued = null)
        {
            double GetWorst((uint pointer, double cost)[] targets)
            {
                var worst = 0d;
                for (var i = 0; i < targets.Length; i++)
                {
                    if (!(targets[i].cost > worst)) continue;
                        
                    worst = targets[i].cost;
                    if (worst >= double.MaxValue) break;
                }

                return worst;
            }
            
            var enumerator = network.GetEdgeEnumerator();
            var paths = new Path[targets.Count];

            _tree.Clear();
            _visits.Clear();
            _heap.Clear();

            // add sources.
            // add forward.
            if (!enumerator.MoveToEdge(source.EdgeId, true)) throw new Exception($"Edge in source {source} not found!");
            var sourceCostForward = getDijkstraWeight(enumerator, Enumerable.Empty<(EdgeId edge, ushort? turn)>()).cost;
            var sourceForwardVisit = uint.MaxValue;
            if (sourceCostForward > 0)
            {
                // can traverse edge in the forward direction.
                var sourceOffsetCostForward = sourceCostForward * (1 - source.OffsetFactor());
                sourceForwardVisit = _tree.AddVisit(enumerator.To, source.EdgeId, enumerator.Head, uint.MaxValue);
                _heap.Push(sourceForwardVisit, sourceOffsetCostForward);
            }

            // add backward.
            if (!enumerator.MoveToEdge(source.EdgeId, false))
                throw new Exception($"Edge in source {source} not found!");
            var sourceCostBackward = getDijkstraWeight(enumerator, Enumerable.Empty<(EdgeId edge, ushort? turn)>()).cost;
            var sourceBackwardVisit = uint.MaxValue;
            if (sourceCostBackward > 0)
            {
                // can traverse edge in the backward direction.
                var sourceOffsetCostBackward = sourceCostBackward * source.OffsetFactor();
                sourceBackwardVisit = _tree.AddVisit(enumerator.To, source.EdgeId, enumerator.Head, uint.MaxValue);
                _heap.Push(sourceBackwardVisit, sourceOffsetCostBackward);
            }

            // add targets.
            var bestTargets = new (uint pointer, double cost)[targets.Count];
            var targetsPerVertex = new Dictionary<VertexId, List<int>>();
            for (var t = 0; t < targets.Count; t++)
            {
                bestTargets[t] = (uint.MaxValue, double.MaxValue);
                var target = targets[t];
                
                // add targets to vertices.
                if (!enumerator.MoveToEdge(target.EdgeId, true)) throw new Exception($"Edge in target {target} not found!");
                if (!targetsPerVertex.TryGetValue(enumerator.From, out var targetsAtVertex))
                {
                    targetsAtVertex = new List<int>();
                    targetsPerVertex[enumerator.From] = targetsAtVertex;
                }
                targetsAtVertex.Add(t);
                if (!targetsPerVertex.TryGetValue(enumerator.To, out targetsAtVertex))
                {
                    targetsAtVertex = new List<int>();
                    targetsPerVertex[enumerator.To] = targetsAtVertex;
                }
                targetsAtVertex.Add(t);
                
                // consider paths 'within' a single edge.
                if (source.EdgeId == target.EdgeId)
                {
                    // the source and target are on the same edge.
                    if (source.Offset == target.Offset)
                    {
                        // source and target are identical.
                        bestTargets[t] = (sourceForwardVisit, 0);
                    }
                    else if (source.Offset < target.Offset &&
                        sourceForwardVisit != uint.MaxValue)
                    {
                        // the source is earlier in the direction of the edge
                        // and the edge can be traversed in this direction.
                        if (!enumerator.MoveToEdge(source.EdgeId, true))
                            throw new Exception($"Edge in source {source} not found!");
                        var weight = getDijkstraWeight(enumerator, Enumerable.Empty<(EdgeId edge, ushort? turn)>()).cost * 
                                     (target.OffsetFactor() - source.OffsetFactor());
                        bestTargets[t] = (sourceForwardVisit, weight);
                    }
                    else if (sourceBackwardVisit != uint.MaxValue)
                    {
                        // the source is earlier against the direction of the edge
                        // and the edge can be traversed in this direction.
                        if (!enumerator.MoveToEdge(source.EdgeId, false))
                            throw new Exception($"Edge in source {source} not found!");
                        var weight = getDijkstraWeight(enumerator, Enumerable.Empty<(EdgeId edge, ushort? turn)>()).cost * 
                                     (source.OffsetFactor() - target.OffsetFactor());
                        bestTargets[t] = (sourceBackwardVisit, weight);
                    }
                }
            }
            
            // update worst target cost.
            var worstTargetCost = GetWorst(bestTargets);

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
                if (currentCost >= worstTargetCost)
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
                    var neighbourEdge = enumerator.Id;
                    if (neighbourEdge == currentVisit.edge) continue;
                    
                    // gets the cost of the current edge.
                    var (neighbourCost, turnCost) = getDijkstraWeight(enumerator, _tree.GetPreviousEdges(currentPointer));
                    if (neighbourCost >= double.MaxValue ||
                        neighbourCost <= 0) continue;

                    // if the vertex has targets, check if this edge is a match.
                    var neighbourPointer = uint.MaxValue;
                    if (targetsAtVertex != null)
                    {
                        // only consider targets when found for the 'from' vertex.
                        // and when this in not a u-turn.
                        foreach (var t in targetsAtVertex)
                        {
                            var target = targets[t];
                            if (target.EdgeId != neighbourEdge) continue;

                            // there is a target on this edge, calculate the cost.
                            // calculate the cost from the 'from' vertex to the target.
                            var targetCost = enumerator.Forward
                                ? neighbourCost * (target.OffsetFactor())
                                : neighbourCost * (1 - target.OffsetFactor());
                            // this is the case where the target is on this edge 
                            // and there is a path to 'from' before.
                            targetCost += currentCost;
                            
                            // add turn cost.
                            targetCost += turnCost;

                            // if this is an improvement, use it!
                            var targetBestCost = bestTargets[t].cost;
                            if (!(targetCost < targetBestCost)) continue;

                            // this is an improvement.
                            neighbourPointer = _tree.AddVisit(enumerator.To,
                                enumerator.Id, enumerator.Head, currentPointer);
                            bestTargets[t] = (neighbourPointer, targetCost);
                            
                            // update worst.
                            worstTargetCost = GetWorst(bestTargets);
                        }
                    }

                    if (queued != null &&
                        queued.Invoke(enumerator.To))
                    { // don't queue this vertex if the queued function returns true.
                        continue;
                    }

                    // add visit if not added yet.
                    if (neighbourPointer == uint.MaxValue) neighbourPointer = _tree.AddVisit(enumerator.To, 
                        enumerator.Id, enumerator.Head, currentPointer);
                    
                    // add visit to heap.
                    _heap.Push(neighbourPointer, neighbourCost + currentCost + turnCost);
                }
            }

            for (var p = 0; p < paths.Length; p++)
            {
                var bestTarget = bestTargets[p];
                if (bestTarget.pointer == uint.MaxValue) continue;

                // build resulting path.
                var path = new Path(network);
                var visit = _tree.GetVisit(bestTarget.pointer);

                // path is at least two edges.
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
                var target = targets[p];
                path.Offset1 = path.First.direction ? source.Offset : (ushort) (ushort.MaxValue - source.Offset);
                path.Offset2 = path.Last.direction
                    ? target.Offset
                    : (ushort) (ushort.MaxValue - target.Offset);

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