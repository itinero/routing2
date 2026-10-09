using System.Collections.Generic;
using Itinero.Network;

namespace Itinero.Routing.Corridors;

/// <summary>
/// Which endpoint half-edge a corridor edge is, if any. The caller derives the offsets.
/// </summary>
public enum CorridorHalf
{
    /// <summary>An ordinary edge, traversed whole.</summary>
    None,

    /// <summary>Out of the origin along the source edge, to its head.</summary>
    SourceForwardToHead,

    /// <summary>Out of the origin against the source edge, to its tail.</summary>
    SourceBackwardToTail,

    /// <summary>Into the destination along the target edge, from its tail.</summary>
    TargetForwardFromTail,

    /// <summary>Into the destination against the target edge, from its head.</summary>
    TargetBackwardFromHead,
}

/// <summary>
/// One state: an edge, its traversal direction, and the vertex arrived at. States rather than
/// vertices because turn costs and the local-access rule depend on how a vertex was reached.
/// </summary>
public readonly record struct CorridorState(EdgeId Edge, bool Forward, VertexId Vertex);

/// <summary>
/// A transition between two corridor states, or between a state and a virtual endpoint node.
/// </summary>
/// <param name="Cost">The head edge's traversal cost plus the turn onto it — not a distance
/// from anything.</param>
/// <param name="Length">Physical length, in the same unit the network reports. For an endpoint
/// half-edge this is the part actually travelled, not the whole edge.</param>
/// <param name="Forward">Which way the edge is travelled, always in path order: tail to head
/// when true, so consecutive edges join head to tail.</param>
public readonly record struct CorridorEdge(
    int Tail,
    int Head,
    double Cost,
    double Length,
    EdgeId Edge,
    bool Forward,
    CorridorHalf Half);

/// <summary>
/// The set of edges that lie on some origin-to-destination path costing no more than a given
/// multiple of the optimum: a cost ellipse around the route, as a graph.
/// </summary>
/// <remarks>
/// Built from the search that answered the route, so it contains that route and prices it the
/// same way. Node 0 is the origin and node 1 the destination; <see cref="States"/> is null at both.
/// </remarks>
public sealed class RouteCorridor
{
    internal RouteCorridor(
        IReadOnlyList<CorridorState?> states,
        IReadOnlyList<CorridorEdge> edges,
        double optimalCost,
        double optimalLength,
        long settled,
        int relaxed,
        int trimmed,
        int fromOrigin,
        int interior,
        int toDestination,
        IReadOnlyList<double> costFromOrigin,
        IReadOnlyList<double> costToDestination)
    {
        this.States = states;
        this.Edges = edges;
        this.OptimalCost = optimalCost;
        this.OptimalLength = optimalLength;
        this.Settled = settled;
        this.Relaxed = relaxed;
        this.Trimmed = trimmed;
        this.FromOrigin = fromOrigin;
        this.Interior = interior;
        this.ToDestination = toDestination;
        this.CostFromOrigin = costFromOrigin;
        this.CostToDestination = costToDestination;
    }

    /// <summary>The origin node's index.</summary>
    public const int SourceNode = 0;

    /// <summary>The destination node's index.</summary>
    public const int TargetNode = 1;

    /// <summary>States by node index; null at the two virtual endpoints.</summary>
    public IReadOnlyList<CorridorState?> States { get; }

    /// <summary>The corridor's edges, in no particular order.</summary>
    public IReadOnlyList<CorridorEdge> Edges { get; }

    /// <summary>What the optimal route cost — the cost the corridor's budget is a multiple of.</summary>
    public double OptimalCost { get; }

    /// <summary>The optimal route's physical length.</summary>
    public double OptimalLength { get; }

    /// <summary>States the route search settled, both halves. Diagnostic.</summary>
    public long Settled { get; }

    /// <summary>Transitions examined before filtering. Diagnostic.</summary>
    public int Relaxed { get; }

    /// <summary>Edges that priced inside the budget but led nowhere. Small; large means the
    /// halves disagree about more than their endpoints' surroundings.</summary>
    public int Trimmed { get; }

    /// <summary>
    /// Edges out of the origin before the trim. With <see cref="Interior"/> and
    /// <see cref="ToDestination"/>, says which stage an empty corridor came up short at.
    /// </summary>
    public int FromOrigin { get; }

    /// <summary>Edges between two states that passed the cost filter, before the trim.</summary>
    public int Interior { get; }

    /// <summary>Edges into the destination that passed the cost filter, before the trim.</summary>
    public int ToDestination { get; }

    /// <summary>
    /// What it costs to reach each node from the origin — the shortest-path potential over the
    /// corridor, in the direction of travel.
    /// </summary>
    /// <remarks>
    /// With <see cref="CostToDestination"/>, <c>CostFromOrigin[tail] + cost +
    /// CostToDestination[head]</c> is the cheapest path through an edge — what the filter tested.
    /// </remarks>
    public IReadOnlyList<double> CostFromOrigin { get; }

    /// <summary>
    /// What it costs to carry on to the destination from each node — the shortest-path potential
    /// over the corridor, computed by the filter and worth keeping rather than recomputing.
    /// </summary>
    public IReadOnlyList<double> CostToDestination { get; }
}
