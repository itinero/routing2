using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Itinero.Algorithms.DataStructures;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.Dijkstra
{
    /// <summary>
    /// A dijkstra implementation.
    /// </summary>
    public class Dijkstra
    {
        private readonly PathTree _tree = new PathTree();
        private readonly HashSet<VertexId> _visits = new HashSet<VertexId>();
        private readonly BinaryHeap<uint> _heap = new BinaryHeap<uint>();

        /// <summary>
        /// Calculates a path.
        /// </summary>
        /// <returns>The path.</returns>
        public Path Run(Graph graph, SnapPoint source,
            IEnumerable<SnapPoint> targets, Func<Graph.Enumerator, uint> getWeight,
            Func<VertexId, bool> settled = null)
        {
            var enumerator = graph.GetEnumerator();
            _tree.Clear();
            _visits.Clear();
            _heap.Clear();
            
            // add sources.
            // add forward.
            if (!enumerator.MoveToEdge(source.EdgeId, true)) throw new Exception($"Edge in source {source} not found!");
            var sourceCostForward = getWeight(enumerator);
            if (sourceCostForward > 0)
            { // can traverse edge in the forward direction.
                var sourceOffsetCostForward = sourceCostForward * (1 - source.OffsetFactor());
                var p = _tree.AddVisit(enumerator.To, source.EdgeId, uint.MaxValue);
                _heap.Push(p, sourceOffsetCostForward);
            }
            // add backward.
            if (!enumerator.MoveToEdge(source.EdgeId, false)) throw new Exception($"Edge in source {source} not found!");
            var sourceCostBackward = getWeight(enumerator);
            if (sourceCostBackward > 0)
            { // can traverse edge in the backward direction.
                var sourceOffsetCostBackward = sourceCostBackward * source.OffsetFactor();
                var p = _tree.AddVisit(enumerator.To, source.EdgeId, uint.MaxValue);
                _heap.Push(p, sourceOffsetCostBackward);
            }
            
            // add targets.
            (uint pointer, float cost, bool forward, SnapPoint target) bestTarget = (uint.MaxValue, float.MaxValue, false, 
                default(SnapPoint));
            var targetMaxCost = 0f;
            var targetsPerVertex = new Dictionary<VertexId, (float cost, bool forward, SnapPoint target)>();
            foreach (var target in targets)
            {
                // add forward.
                if (!enumerator.MoveToEdge(target.EdgeId, true)) throw new Exception($"Edge in target {target} not found!");
                var targetCostForward = getWeight(enumerator);
                if (targetCostForward > 0)
                { // can traverse edge in the forward direction, we can the from vertex.
                    var targetCostForwardOffset = targetCostForward * target.OffsetFactor();
                    targetsPerVertex[enumerator.From] = (targetCostForwardOffset, true, target);
                    if (targetCostForwardOffset > targetMaxCost)
                    {
                        targetMaxCost = targetCostForwardOffset;
                    }
                }
                // add backward.
                if (!enumerator.MoveToEdge(target.EdgeId, false)) throw new Exception($"Edge in target {target} not found!");
                var targetCostBackward = getWeight(enumerator);
                if (targetCostBackward > 0)
                { // can traverse edge in the forward direction, we can the from vertex.
                    var targetCostBackwardOffset = targetCostBackward * target.OffsetFactor();
                    targetsPerVertex[enumerator.From] = (targetCostBackwardOffset, false, target);
                    if (targetCostBackwardOffset > targetMaxCost)
                    {
                        targetMaxCost = targetCostBackwardOffset;
                    }
                }
            }
            
            // keep going until heap is empty.
            while (_heap.Count > 0)
            {
                // dequeue new visit.
                var currentPointer = _heap.Pop(out var currentCost);
                var currentVisit = _tree.GetVisit(currentPointer);
                while (_visits.Contains(currentVisit.vertex))
                { // visited before, skip.
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
                { // break if requested.
                    break;
                }
                
                // check if the search needs to stop.
                if (currentCost + targetMaxCost > bestTarget.cost)
                { // impossible to improve on cost to any target.
                    break;
                }
                
                // check if this is a target.
                if (targetsPerVertex.TryGetValue(currentVisit.vertex, out var targetDetails))
                { // this vertex is a target, check for an improvement.
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
                { // no edges, move on!
                    continue;
                }

                while (enumerator.MoveNext())
                {
                    var neighbourCost = getWeight(enumerator);
                    if (neighbourCost >= float.MaxValue || 
                        neighbourCost <= 0) continue;

                    var neighbourEdge = enumerator.Id;
                    if (neighbourEdge == currentVisit.edge) continue; // don't consider u-turns.
                    
                    var neighbourPointer = _tree.AddVisit(enumerator.To, enumerator.Id, currentPointer);
                    _heap.Push(neighbourPointer, neighbourCost + currentCost);
                }
            }

            if (bestTarget.pointer == uint.MaxValue) return null;

            var path = new Path(graph);
            var visit = _tree.GetVisit(bestTarget.pointer);
            
            if (!enumerator.MoveToEdge(bestTarget.target.EdgeId, bestTarget.forward)) throw new Exception($"Edge in bestTarget {bestTarget} not found!");
            path.Prepend(bestTarget.target.EdgeId, enumerator.To);
            while (true)
            {
                path.Prepend(visit.edge, visit.vertex);

                if (visit.previousPointer == uint.MaxValue)
                {
                    break;
                }
                visit = _tree.GetVisit(visit.previousPointer);
            }

            return path;
        }

        private static readonly ThreadLocal<Dijkstra> DefaultLazy = new ThreadLocal<Dijkstra>(() => new Dijkstra());
        
        /// <summary>
        /// Gets the default dijkstra instance (for the local thread).
        /// </summary>
        public static Dijkstra Default => DefaultLazy.Value;
    }
}