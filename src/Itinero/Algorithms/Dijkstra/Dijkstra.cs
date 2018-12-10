using System.Collections.Generic;
using System.Linq;
using Itinero.Algorithms.DataStructures;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.Dijkstra
{
    internal class Dijkstra
    {
        private readonly IEnumerable<(VertexId vertex, uint edge, float cost)> _sources;
        private readonly IEnumerable<(VertexId vertex, uint edge, float cost)> _targets;
        private readonly Graph _graph;
        
        public Dijkstra(Graph graph, IEnumerable<(VertexId vertex, uint edge, float cost)> sources,
            IEnumerable<(VertexId vertex, uint edge, float cost)> targets)
        {
            _graph = graph;
            _sources = sources;
            _targets = targets;
        }

        /// <summary>
        /// Calculates a path.
        /// </summary>
        /// <returns>The path.</returns>
        public Path Run()
        {
            var enumerator = _graph.GetEnumerator();
            var tree = new PathTree();
            var visits = new HashSet<VertexId>();
            var heap = new BinaryHeap<uint>();

            // push sources onto heap.
            foreach (var source in _sources)
            {
                var p = tree.AddVisit(source.vertex, source.edge, uint.MaxValue);
                heap.Push(p, source.cost);
            }
            
            // create a hashset of targets.
            (uint pointer, float cost) bestTarget = (uint.MaxValue, float.MaxValue);
            var targetMaxCost = 0f;
            var targets = new Dictionary<VertexId, float>();
            foreach (var target in _targets)
            {
                targets[target.vertex] = target.cost;
                if (target.cost > targetMaxCost)
                {
                    targetMaxCost = target.cost;
                }
            }
            
            // keep going until heap is empty.
            while (heap.Count > 0)
            {
                // dequeue new visit.
                var currentPointer = heap.Pop(out var currentCost);
                var currentVisit = tree.GetVisit(currentPointer);
                while (visits.Contains(currentVisit.vertex))
                { // visited before, skip.
                    currentPointer = uint.MaxValue;
                    if (heap.Count == 0)
                    {
                        break;
                    }

                    currentPointer = heap.Pop(out currentCost);
                    currentVisit = tree.GetVisit(currentPointer);
                }

                if (currentPointer == uint.MaxValue)
                {
                    break;
                }

                // log visit.
                visits.Add(currentVisit.vertex);
                
                // check if the search needs to stop.
                if (currentCost + targetMaxCost > bestTarget.cost)
                { // impossible to improve on cost to any target.
                    break;
                }
                
                // check if this is a target.
                if (targets.TryGetValue(currentVisit.vertex, out var targetCost))
                { // this vertex is a target, check for an improvement.
                    targetCost += currentCost;
                    if (targetCost < bestTarget.cost)
                    {
                        bestTarget = (currentPointer, targetCost);
                    }

                    targets.Remove(currentVisit.vertex);
                }
                
                // check neighbours.
                if (!enumerator.MoveTo(currentVisit.vertex))
                { // no edges, move on!
                    continue;
                }

                while (enumerator.MoveNext())
                {
                    var neighbourCost = this.Cost(enumerator);
                    if (neighbourCost >= float.MaxValue) continue;

                    var neighbourEdge = enumerator.Id;
                    if (neighbourEdge == currentVisit.edge) continue; // don't consider u-turns.
                    
                    var neighbourPointer = tree.AddVisit(enumerator.To, enumerator.Id, currentPointer);
                    heap.Push(neighbourPointer, neighbourCost);
                }
            }

            if (bestTarget.pointer == uint.MaxValue) return null;

            var path = new Path(_graph);
            var visit = tree.GetVisit(bestTarget.pointer);
            while (true)
            {
                path.Prepend(visit.edge, visit.vertex);

                if (visit.previousPointer == uint.MaxValue)
                {
                    break;
                }
                visit = tree.GetVisit(visit.previousPointer);
            }

            return path;
        }

        protected virtual float Cost(Graph.Enumerator graph)
        {
            return 1;
        }
    }
}