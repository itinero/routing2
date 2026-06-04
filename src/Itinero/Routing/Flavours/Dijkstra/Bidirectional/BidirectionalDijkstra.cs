using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routes.Paths;
using Itinero.Routing.Costs;
using Itinero.Routing.DataStructures;
using Itinero.Snapping;

namespace Itinero.Routing.Flavours.Dijkstra.Bidirectional;

/// <summary>
/// Edge-based bidirectional Dijkstra. Two single-source edge-based searches grow
/// simultaneously — forward from the source, backward from the target. Visits in
/// both halves are dedup'd by <c>(edge, vertex)</c>, so different incoming edges at
/// a vertex retain distinct best-cost state. They meet wherever a forward-settled
/// <c>(E_f, V)</c> shares a vertex V with a backward-settled <c>(E_b, V)</c> and the
/// turn at V from E_f to E_b is allowed.
///
/// When <c>isMainN</c> is supplied each half applies the same per-half access rule
/// in its own search direction: reject relaxations where the previous edge is main-N
/// and the candidate next edge is not. The two halves combined admit exactly the
/// five valid path shapes the unidirectional state-bit Dijkstra admits.
/// </summary>
internal class BidirectionalDijkstra
{
    private readonly Half _forward = new();
    private readonly Half _backward = new();

    // Best meeting found so far.
    private double _bestCost;
    private uint _bestForward;
    private uint _bestBackward;
    // Path returned by the same-edge single-hop fast path; bypasses meeting.
    private Path? _singleHopPath;

    private sealed class Half
    {
        public readonly PathTree Tree = new();
        public readonly BinaryHeap<(uint pointer, EdgeId edge, VertexId vertex)> Heap = new();
        public readonly HashSet<(EdgeId edge, VertexId vertex)> Settled = new();
        // Per-vertex list of settled arrivals. Each entry records the incoming edge plus
        // the head order at this vertex for that edge — both are needed to query turn
        // costs against this label when the other half asks "can I meet you here?".
        public readonly Dictionary<VertexId, List<SettledEntry>> SettledByVertex = new();

        public void Clear()
        {
            Tree.Clear();
            Heap.Clear();
            Settled.Clear();
            SettledByVertex.Clear();
        }
    }

    private readonly record struct SettledEntry(EdgeId Edge, bool Forward, byte? HeadOrder, uint Pointer, double Cost);

    /// <summary>Fresh instance per call. See <see cref="Dijkstra.Default"/> for rationale.</summary>
    public static BidirectionalDijkstra Default => new();

    public async Task<(Path? path, double cost)> RunAsync(
        RoutingNetwork network,
        SnapPoint source,
        SnapPoint target,
        ICostFunction costFunction,
        Func<VertexId, Task<bool>>? settledCb = null,
        CancellationToken cancellationToken = default,
        IsMainNFunc? isMainN = null)
    {
        _forward.Clear();
        _backward.Clear();
        _bestCost = double.MaxValue;
        _bestForward = uint.MaxValue;
        _bestBackward = uint.MaxValue;
        _singleHopPath = null;

        // Single-edge fast path. If both snap points are on the same edge a direct sub-edge
        // path may already win; we seed _bestCost so the bidirectional search can still
        // improve via a longer path that happens to have lower cost.
        if (network.TrySingleHop(source, target, costFunction, out var singleHopPath, out var singleHopCost))
        {
            _singleHopPath = singleHopPath;
            _bestCost = singleHopCost;
        }

        var enumerator = network.GetEdgeEnumerator();

        PushTerminal(enumerator, source, costFunction, _forward, asOrigin: true);
        PushTerminal(enumerator, target, costFunction, _backward, asOrigin: false);

        var forwardCost = 0.0;
        var backwardCost = 0.0;

        while (_forward.Heap.Count > 0 || _backward.Heap.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Stopping: once both heap mins combined exceed best, no improvement possible.
            if (_bestCost <= forwardCost + backwardCost) break;

            if (_forward.Heap.Count > 0)
            {
                var popped = await this.Step(network, _forward, _backward, isForwardHalf: true, costFunction, isMainN, settledCb, cancellationToken);
                if (popped.HasValue) forwardCost = popped.Value;
            }
            if (_backward.Heap.Count > 0)
            {
                var popped = await this.Step(network, _backward, _forward, isForwardHalf: false, costFunction, isMainN, settledCb, cancellationToken);
                if (popped.HasValue) backwardCost = popped.Value;
            }
        }

        if (_bestCost >= double.MaxValue) return (null, double.MaxValue);

        // Single-hop won.
        if (_bestForward == uint.MaxValue) return (_singleHopPath, _bestCost);

        var forwardPath = BuildPath(network, _forward.Tree, _bestForward);
        var backwardPath = BuildPath(network, _backward.Tree, _bestBackward);
        forwardPath.Append(backwardPath.InvertDirection());

        forwardPath.Offset1 = forwardPath.First.direction ? source.Offset : (ushort)(ushort.MaxValue - source.Offset);
        forwardPath.Offset2 = forwardPath.Last.direction ? target.Offset : (ushort)(ushort.MaxValue - target.Offset);

        return (forwardPath, _bestCost);
    }

    private static void PushTerminal(
        RoutingNetworkEdgeEnumerator enumerator,
        SnapPoint snap,
        ICostFunction costFunction,
        Half half,
        bool asOrigin)
    {
        // Mirror of the vertex-based DijkstraAlgorithmExtensions.Push contract:
        //   asOrigin=true  → cost computed in the edge's natural direction (tailToHead = true);
        //   asOrigin=false → cost computed against the edge (tailToHead = false), so the
        //                    backward search reaches "back toward source" with the right weights.
        foreach (var forward in new[] { true, false })
        {
            if (!enumerator.MoveTo(snap.EdgeId, forward)) continue;
            var (canAccess, _, localAccess, cost, _) = costFunction.Get(enumerator, tailToHead: asOrigin, null);
            if (!canAccess || cost <= 0) continue;
            var offsetCost = forward
                ? cost * (1 - snap.OffsetFactor())
                : cost * snap.OffsetFactor();
            var p = half.Tree.AddVisit(enumerator, leftMain: false, localAccess: localAccess, uint.MaxValue);
            half.Heap.Push((p, enumerator.EdgeId, enumerator.Head), offsetCost);
        }
    }

    private async Task<double?> Step(
        RoutingNetwork network,
        Half active,
        Half other,
        bool isForwardHalf,
        ICostFunction costFunction,
        IsMainNFunc? isMainN,
        Func<VertexId, Task<bool>>? settledCb,
        CancellationToken cancellationToken)
    {
        // Dequeue, skipping already-settled (edge, vertex) labels.
        var entry = active.Heap.Pop(out var cost);
        while (active.Settled.Contains((entry.edge, entry.vertex)))
        {
            if (active.Heap.Count == 0) return cost;
            entry = active.Heap.Pop(out cost);
        }
        if (!active.Settled.Add((entry.edge, entry.vertex))) return cost;

        var (vertex, edge, forward, _, localAccess, headOrder, _) = active.Tree.GetVisitWithState(entry.pointer);

        // Record in per-vertex settled multimap for meeting checks initiated by the other half.
        if (!active.SettledByVertex.TryGetValue(vertex, out var list))
        {
            list = new List<SettledEntry>(2);
            active.SettledByVertex[vertex] = list;
        }
        list.Add(new SettledEntry(edge, forward, headOrder, entry.pointer, cost));

        // Settled callback semantics match the unidirectional edge-based: returning true
        // means "stop expanding from this vertex" (e.g. outside the max-distance box).
        if (settledCb != null && await settledCb(vertex)) return cost;
        if (cancellationToken.IsCancellationRequested) return cost;

        // Meeting check against already-settled labels at this vertex in the other half.
        this.TryMeet(network, costFunction, isForwardHalf, edge, forward, headOrder, entry.pointer, cost, vertex, other);

        // Expand neighbours.
        var probe = network.GetEdgeEnumerator();
        if (!probe.MoveTo(vertex)) return cost;
        while (probe.MoveNext())
        {
            var neighbourEdge = probe.EdgeId;
            if (neighbourEdge == edge) continue; // no U-turn

            // Cost in this half's direction. Forward uses tailToHead=true, backward uses false.
            // PreviousEdgeEnumerable provides turn-cost context from the path so far.
            var prev = new PreviousEdgeEnumerable(active.Tree, entry.pointer);
            var (canAccess, _, neighbourLocalAccess, neighbourCost, turnCost) =
                costFunction.Get(probe, tailToHead: isForwardHalf, prev);
            if (!canAccess || neighbourCost is >= double.MaxValue or <= 0) continue;
            if (turnCost is >= double.MaxValue or < 0) continue;

            // Per-half access-aware rule: reject prev_main && !curr_main in this half's direction.
            if (isMainN != null)
            {
                var prevMain = IsMain(edge, localAccess, isMainN);
                var currMain = IsMain(neighbourEdge, neighbourLocalAccess, isMainN);
                if (prevMain && !currMain) continue;
            }

            var totalCost = cost + neighbourCost + turnCost;
            if (totalCost >= _bestCost) continue;

            var neighbourPointer = active.Tree.AddVisit(probe, leftMain: false, localAccess: neighbourLocalAccess, entry.pointer);

            // Meeting check against the other half's settled set BEFORE pushing — mirrors the
            // existing vertex-based pattern's OnQueued hook.
            this.TryMeet(network, costFunction, isForwardHalf, neighbourEdge, probe.Forward, probe.HeadOrder,
                neighbourPointer, totalCost, probe.Head, other);

            active.Heap.Push((neighbourPointer, neighbourEdge, probe.Head), totalCost);
        }

        return cost;
    }

    private void TryMeet(
        RoutingNetwork network,
        ICostFunction costFunction,
        bool isForwardHalf,
        EdgeId edge,
        bool forward,
        byte? headOrder,
        uint pointer,
        double cost,
        VertexId vertex,
        Half other)
    {
        if (!other.SettledByVertex.TryGetValue(vertex, out var settledList)) return;

        foreach (var entry in settledList)
        {
            if (entry.Edge == edge) continue; // U-turn

            // Quick early-out before computing the turn cost.
            var combinedNoTurn = cost + entry.Cost;
            if (combinedNoTurn >= _bestCost) continue;

            // Determine the path-incoming and path-outgoing edge at the meeting vertex:
            //   - Path-incoming is the FORWARD half's settled edge at V (forward arrived here via it).
            //   - Path-outgoing is the BACKWARD half's settled edge at V (path order continues via it
            //     toward target; backward search came from target via that edge).
            // When the active half is forward, `edge` is path-incoming and `entry.Edge` is outgoing;
            // when active half is backward, the roles swap.
            EdgeId pathIncoming;
            byte? pathIncomingHeadOrder;
            EdgeId pathOutgoing;
            bool pathOutgoingForwardFromOther;
            if (isForwardHalf)
            {
                pathIncoming = edge;
                pathIncomingHeadOrder = headOrder;
                pathOutgoing = entry.Edge;
                pathOutgoingForwardFromOther = entry.Forward;
            }
            else
            {
                pathIncoming = entry.Edge;
                pathIncomingHeadOrder = entry.HeadOrder;
                pathOutgoing = edge;
                pathOutgoingForwardFromOther = forward;
            }

            var turnCost = TurnCostAtMeeting(network, costFunction, vertex,
                pathIncoming, pathIncomingHeadOrder,
                pathOutgoing, pathOutgoingForwardFromOther);
            if (turnCost is >= double.MaxValue or < 0) continue;

            var combined = combinedNoTurn + turnCost;
            if (combined >= _bestCost) continue;

            _bestCost = combined;
            if (isForwardHalf)
            {
                _bestForward = pointer;
                _bestBackward = entry.Pointer;
            }
            else
            {
                _bestForward = entry.Pointer;
                _bestBackward = pointer;
            }
        }
    }

    /// <summary>
    /// Turn cost at the meeting vertex from the path-incoming edge to the path-outgoing edge.
    /// The backward search's enumerator visited <paramref name="pathOutgoing"/> with V as the
    /// head; in path order V is the tail of pathOutgoing, so the path-outgoing traversal is
    /// the opposite of the backward enumerator's direction.
    /// </summary>
    private static double TurnCostAtMeeting(
        RoutingNetwork network,
        ICostFunction costFunction,
        VertexId vertex,
        EdgeId pathIncoming,
        byte? pathIncomingHeadOrder,
        EdgeId pathOutgoing,
        bool pathOutgoingForwardFromOther)
    {
        var probe = network.GetEdgeEnumerator();
        // Position at the outgoing edge in path-outgoing direction (V is tail).
        if (!probe.MoveTo(pathOutgoing, !pathOutgoingForwardFromOther)) return double.MaxValue;

        var previous = pathIncomingHeadOrder.HasValue
            ? new (EdgeId edgeId, byte? turn)[] { (pathIncoming, pathIncomingHeadOrder) }
            : null;
        var (_, _, _, _, turnCost) = costFunction.Get(probe, tailToHead: true, previous);
        return turnCost;
    }

    private static bool IsMain(EdgeId edgeId, bool localAccess, IsMainNFunc isMainN)
    {
        var verdict = isMainN(edgeId, localAccess);
        // Without a leftMain state-bit the bidirectional defaults unknown to "main": the per-half
        // rule then doesn't spuriously reject across edges we know nothing about.
        return verdict ?? true;
    }

    private static Path BuildPath(RoutingNetwork network, PathTree tree, uint pointer)
    {
        var path = new Path(network);
        var visit = tree.GetVisit(pointer);
        while (true)
        {
            path.Prepend(visit.edge, visit.forward);
            if (visit.previousPointer == uint.MaxValue) break;
            visit = tree.GetVisit(visit.previousPointer);
        }
        return path;
    }
}
