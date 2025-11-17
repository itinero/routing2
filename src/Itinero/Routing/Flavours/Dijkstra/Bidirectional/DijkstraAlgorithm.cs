using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.DataStructures;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;

internal abstract class DijkstraAlgorithm
{
    private readonly PathTree _tree = new();
    private readonly Dictionary<VertexId, (uint p, double cost)> _settled = [];
    private readonly BinaryHeap<uint> _heap = new();
    protected readonly RoutingNetworkEdgeEnumerator _enumerator;
    protected readonly RoutingNetwork _network;

    protected DijkstraAlgorithm(RoutingNetwork network)
    {
        _network = network;

        _enumerator = network.GetEdgeEnumerator();
    }

    internal RoutingNetwork RoutingNetwork => _network;

    protected abstract bool OnQueued(uint visit, EdgeId edge, (double cost, double turnCost) edgeCost, VertexId vertex, double totalCost);

    protected abstract bool OnSettled(uint visit, VertexId vertex, double cost);

    protected abstract (double cost, double turnCost) GetCost(RoutingNetworkEdgeEnumerator edgeEnumerator,
        IEnumerable<(EdgeId edge, byte? turn)> previousEdges);

    internal bool TryGetVisit(VertexId vertex, out (uint p, double cost) visit)
    {
        return _settled.TryGetValue(vertex, out visit);
    }

    internal void Clear()
    {
        _heap.Clear();
        _tree.Clear();
        _settled.Clear();
    }

    internal (VertexId vertex, EdgeId edge, bool forward, byte? head, uint previousPointer) GetVisit(uint pointer)
    {
        return _tree.GetVisit(pointer);
    }

    internal uint Push(EdgeId edgeId, bool forward, double cost)
    {
        if (!_enumerator.MoveTo(edgeId, forward)) throw new Exception($"Edge not found!");

        var v = _tree.AddVisit(_enumerator, uint.MaxValue);
        _heap.Push(v, cost);
        return v;
    }

    internal (uint pointer, (VertexId vertex, EdgeId edge, bool forward, byte? head, uint previousPointer) visit, double cost) Pop()
    {
        var currentPointer = _heap.Pop(out var currentCost);
        var currentVisit = _tree.GetVisit(currentPointer);
        while (!_settled.TryAdd(currentVisit.vertex, (currentPointer, currentCost)))
        {
            currentPointer = uint.MaxValue;
            if (_heap.Count == 0) break;

            currentPointer = _heap.Pop(out currentCost);
            currentVisit = _tree.GetVisit(currentPointer);
        }

        return (currentPointer, currentVisit, currentCost);
    }

    internal IEnumerable<(EdgeId edge, byte? turn)> GetPreviousEdges(uint pointer, int maxCount = 16)
    {
        using var previous = _tree.GetPreviousEdges(pointer).GetEnumerator();
        while (previous.MoveNext())
        {
            if (maxCount <= 0) yield break;
            maxCount--;

            yield return previous.Current;
        }
    }

    internal bool CanTurn(VertexId vertex,
        IReadOnlyList<(EdgeId edge, byte? turn)> previousEdges, IReadOnlyList<EdgeId> nextEdges)
    {
        if (nextEdges.Count == 0) throw new Exception("Not a turn");
        if (previousEdges.Count == 0) throw new Exception("Not a turn");

        // no U-turns
        if (nextEdges[0] == previousEdges[0].edge) return false;

        // check neighbours.
        var nextChecked = 0;
        var edges = previousEdges.ToList();
        while (nextChecked < nextEdges.Count)
        {
            var nextEdge = nextEdges[nextChecked];
            var edgeFound = false;

            if (!_enumerator.MoveTo(vertex)) return false;
            while (_enumerator.MoveNext())
            {
                // filter out if U-turns or visits on the same edge.
                var neighbourEdge = _enumerator.EdgeId;
                if (neighbourEdge != nextEdge) continue;

                // gets the cost of the current edge.
                var (_, turnCost) = this.GetCost(_enumerator, edges);

                if (turnCost is >= double.MaxValue or < 0) return false;

                edges.Add((_enumerator.EdgeId, _enumerator.HeadOrder));
                vertex = _enumerator.Head;
                edgeFound = true;
                break;
            }

            if (!edgeFound) return false;

            nextChecked++;
        }

        // if all edges are checked, the turn is possible.
        if (nextChecked >= nextEdges.Count) return true;

        // target edge not found as neighbour.
        return false;
    }

    internal bool Step(uint pointer, (VertexId vertex, EdgeId edge, bool forward, byte? head, uint previousPointer) visit, double cost)
    {
        // log settled and see if we need to continue.
        if (!this.OnSettled(pointer, visit.vertex, cost)) return false;

        // check neighbours.
        if (!_enumerator.MoveTo(visit.vertex)) return true;
        while (_enumerator.MoveNext())
        {
            // filter out if U-turns or visits on the same edge.
            var neighbourEdge = _enumerator.EdgeId;
            if (neighbourEdge == visit.edge) continue;

            // gets the cost of the current edge.
            var (neighbourCost, turnCost) = this.GetCost(_enumerator, _tree.GetPreviousEdges(pointer));

            // ignore if cost is 0 or infinite.
            if (neighbourCost is >= double.MaxValue or <= 0) continue;
            if (turnCost is >= double.MaxValue or < 0) continue;

            // check if the vertex has to be queued.
            var totalCost = neighbourCost + cost + turnCost;

            // add visit if not added yet.
            var neighbourPointer = _tree.AddVisit(_enumerator, pointer);

            // call the on queued method and allow checking for stopping conditions.
            if (!this.OnQueued(neighbourPointer, _enumerator.EdgeId, (neighbourCost, turnCost), _enumerator.Head, totalCost)) continue;

            // add visit to heap.
            _heap.Push(neighbourPointer, totalCost);
        }

        return true;
    }
}
