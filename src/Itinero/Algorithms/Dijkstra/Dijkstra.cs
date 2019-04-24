using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Algorithms.DataStructures;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.Dijkstra
{
    public class Dijkstra
    {
        /// <summary>
        /// Calculates a path.
        /// </summary>
        /// <returns>The path.</returns>
        public Path Run(Graph graph, IEnumerable<(VertexId vertex, uint edge, float cost)> sources,
            IEnumerable<(VertexId vertex, uint edge, float cost)> targets, Func<Graph.Enumerator, uint> getFactor)
        {
            var enumerator = graph.GetEnumerator();
            var tree = new PathTree();
            var visits = new HashSet<VertexId>();
            var heap = new BinaryHeap<uint>();

            // push sources onto heap.
            foreach (var source in sources)
            {
                var p = tree.AddVisit(source.vertex, source.edge, uint.MaxValue);
                heap.Push(p, source.cost);
            }
            
            // create a hashset of targets.
            (uint pointer, float cost) bestTarget = (uint.MaxValue, float.MaxValue);
            var targetMaxCost = 0f;
            var targetsPerVertex = new Dictionary<VertexId, float>();
            foreach (var target in targets)
            {
                targetsPerVertex[target.vertex] = target.cost;
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
                if (targetsPerVertex.TryGetValue(currentVisit.vertex, out var targetCost))
                { // this vertex is a target, check for an improvement.
                    targetCost += currentCost;
                    if (targetCost < bestTarget.cost)
                    {
                        bestTarget = (currentPointer, targetCost);
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
                    var neighbourCost = getFactor(enumerator);
                    if (neighbourCost >= float.MaxValue) continue;

                    var neighbourEdge = enumerator.Id;
                    if (neighbourEdge == currentVisit.edge) continue; // don't consider u-turns.
                    
                    var neighbourPointer = tree.AddVisit(enumerator.To, enumerator.Id, currentPointer);
                    heap.Push(neighbourPointer, neighbourCost);
                }
            }

            if (bestTarget.pointer == uint.MaxValue) return null;

            var path = new Path(graph);
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
    }
}