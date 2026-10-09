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
    // Not readonly: a search can be handed halves that someone else already expanded. See
    // ContinueAsync.
    private SearchHalf _forward = new(isForwardHalf: true);
    private SearchHalf _backward = new(isForwardHalf: false);

    // Best meeting found so far.
    private double _bestCost;

    /// <summary>
    /// How far past the best meeting the search keeps relaxing, as a multiple of it. 1.0 is a
    /// route; above that it keeps the transitions a caller mapping the region around it needs.
    /// </summary>
    private double _pruneFactor = 1.0;

    /// The ceiling relaxations are pruned at, and the stopping threshold.
    private double PruneCost =>
        _bestCost >= double.MaxValue ? double.MaxValue : _bestCost * _pruneFactor;
    private uint _bestForward;
    private uint _bestBackward;
    // Path returned by the same-edge single-hop fast path; bypasses meeting.
    private Path? _singleHopPath;

    /// <summary>Fresh instance per call. See <see cref="Dijkstra.Default"/> for rationale.</summary>
    public static BidirectionalDijkstra Default => new();

    public async Task<(Path? path, double cost)> RunAsync(
        RoutingNetwork network,
        SnapPoint source,
        SnapPoint target,
        ICostFunction costFunction,
        Func<VertexId, Task<bool>>? settledCb = null,
        CancellationToken cancellationToken = default,
        IsMainNFunc? isMainN = null,
        HeuristicFunc? potential = null,
        bool localAccessRule = false)
    {
        _forward = new SearchHalf(isForwardHalf: true);
        _backward = new SearchHalf(isForwardHalf: false);

        return await this.RunCoreAsync(network, source, target, costFunction, seedTerminals: true,
            settledCb, cancellationToken, isMainN, potential, localAccessRule);
    }

    /// <summary>
    /// Runs to completion from two halves someone else has already expanded.
    /// </summary>
    /// <remarks>
    /// The halves come from snapping, which had to expand outward anyway. The snap points are
    /// still needed, not to seed but because the path's end offsets come from them.
    /// </remarks>
    public async Task<(Path? path, double cost)> ContinueAsync(
        RoutingNetwork network,
        SearchHalf forward,
        SearchHalf backward,
        SnapPoint source,
        SnapPoint target,
        ICostFunction costFunction,
        Func<VertexId, Task<bool>>? settledCb = null,
        CancellationToken cancellationToken = default,
        IsMainNFunc? isMainN = null,
        HeuristicFunc? potential = null,
        bool localAccessRule = false,
        ICostFunction? forwardCostFunction = null,
        ICostFunction? backwardCostFunction = null,
        double pruneFactor = 1.0,
        RelaxedCallback? onForwardRelaxed = null,
        RelaxedCallback? onBackwardRelaxed = null)
    {
        _forward = forward;
        _backward = backward;

        return await this.RunCoreAsync(network, source, target, costFunction, seedTerminals: false,
            settledCb, cancellationToken, isMainN, potential, localAccessRule,
            forwardCostFunction, backwardCostFunction,
            pruneFactor, onForwardRelaxed, onBackwardRelaxed);
    }

    private async Task<(Path? path, double cost)> RunCoreAsync(
        RoutingNetwork network,
        SnapPoint source,
        SnapPoint target,
        ICostFunction costFunction,
        bool seedTerminals,
        Func<VertexId, Task<bool>>? settledCb,
        CancellationToken cancellationToken,
        IsMainNFunc? isMainN,
        HeuristicFunc? potential,
        bool localAccessRule,
        ICostFunction? forwardCostFunction = null,
        ICostFunction? backwardCostFunction = null,
        double pruneFactor = 1.0,
        RelaxedCallback? onForwardRelaxed = null,
        RelaxedCallback? onBackwardRelaxed = null)
    {
        _pruneFactor = pruneFactor < 1.0 ? 1.0 : pruneFactor;

        // Each half can carry its own pocket relabelling, so the two endpoints are independent.
        var forwardCostFn = forwardCostFunction ?? costFunction;
        var backwardCostFn = backwardCostFunction ?? costFunction;
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

        // Balanced potentials sum to zero at every vertex, which keeps the stopping rule
        // below valid: at any meeting vertex the two keys still add up to the true cost.
        HeuristicFunc? forwardPotential = null;
        HeuristicFunc? backwardPotential = null;
        if (potential != null)
        {
            forwardPotential = potential;
            backwardPotential = (longitude, latitude) => -potential(longitude, latitude);
        }

        if (seedTerminals)
        {
            _forward.PushTerminal(enumerator, source, forwardCostFn, forwardPotential);
            _backward.PushTerminal(enumerator, target, backwardCostFn, backwardPotential);
        }
        else if (potential != null)
        {
            // An adopted half was keyed with no potential; mixing orderings settles states out
            // of order. Re-keying costs O(n log n) in a frontier bounded by the expansion.
            _forward.ReKey(network, forwardPotential);
            _backward.ReKey(network, backwardPotential);
        }

        // Built once, not per step: the search runs these millions of times and a fresh closure
        // each time would be pure allocation.
        var onForwardReached = new ReachedCallback((edge, fwd, headOrder, pointer, cost, vertex) =>
            this.TryMeet(network, costFunction, isForwardHalf: true, edge, fwd, headOrder, pointer,
                cost, vertex, _backward));
        var onBackwardReached = new ReachedCallback((edge, fwd, headOrder, pointer, cost, vertex) =>
            this.TryMeet(network, costFunction, isForwardHalf: false, edge, fwd, headOrder, pointer,
                cost, vertex, _forward));

        if (!seedTerminals)
        {
            // Adopted halves never asked whether they had already met. For two endpoints in one
            // pocket that is the whole answer, and without this the search reports no route.
            foreach (var (vertex, arrivals) in _forward.SettledByVertex)
            {
                if (!_backward.SettledByVertex.ContainsKey(vertex)) continue;

                foreach (var arrival in arrivals)
                {
                    this.TryMeet(network, costFunction, isForwardHalf: true, arrival.Edge,
                        arrival.Forward, arrival.HeadOrder, arrival.Pointer, arrival.Cost, vertex,
                        _backward);
                }
            }
        }

        _forward.OtherEndpointEdge = target.EdgeId;
        _backward.OtherEndpointEdge = source.EdgeId;

        var forwardCost = 0.0;
        var backwardCost = 0.0;

        while (_forward.Heap.Count > 0 || _backward.Heap.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Once both heap mins combined exceed the ceiling, nothing left can come in under it.
            if (this.PruneCost <= forwardCost + backwardCost) break;

            if (_forward.Heap.Count > 0)
            {
                var popped = await _forward.StepAsync(network, forwardCostFn, isMainN, localAccessRule,
                    this.PruneCost, settledCb, forwardPotential, onForwardReached, cancellationToken,
                    onForwardRelaxed);
                if (popped.HasValue) forwardCost = popped.Value;
            }
            if (_backward.Heap.Count > 0)
            {
                var popped = await _backward.StepAsync(network, backwardCostFn, isMainN, localAccessRule,
                    this.PruneCost, settledCb, backwardPotential, onBackwardReached, cancellationToken,
                    onBackwardRelaxed);
                if (popped.HasValue) backwardCost = popped.Value;
            }

            // One half has run out of anywhere to go and never got to the other endpoint, so
            // it has settled everything reachable from its own and the other endpoint is not
            // among it. The two cannot meet.
            if (_forward.Heap.Count == 0 && !_forward.ReachedOtherEndpoint) break;
            if (_backward.Heap.Count == 0 && !_backward.ReachedOtherEndpoint) break;
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
        SearchHalf other)
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

            var turnCost = SearchHalf.TurnCostBetween(network.GetEdgeEnumerator(), costFunction,
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
