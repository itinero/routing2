using System.Runtime.CompilerServices;
using Itinero.Indexes;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Profiles;
using Itinero.Routing.Costs.Caches;
using Itinero.Routing.Flavours.Dijkstra;

namespace Itinero.Routing.Costs;

/// <summary>
/// A profile plus the edge type index, memoising the factor per type id.
/// </summary>
/// <remarks>
/// There is no uncached counterpart: a factor is a function of the edge type, so the memo is
/// bounded by the type count and fills itself from <c>GetEdgeType</c> without touching an edge.
/// </remarks>
internal class ProfileCostFunction : ICostFunction
{
    private readonly Profile _profile;
    private readonly IAttributeSetIndex _edgeTypes;
    private readonly IAttributeSetIndex _turnCostTypes;
    private readonly EdgeFactorCache _edgeFactorCache = new();
    private readonly TurnCostFactorCache _turnCostFactorCache = new();

    internal ProfileCostFunction(Profile profile, IAttributeSetIndex edgeTypes,
        IAttributeSetIndex turnCostTypes)
    {
        _profile = profile;
        _edgeTypes = edgeTypes;
        _turnCostTypes = turnCostTypes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EdgeFactor GetFactor(uint edgeTypeId)
    {
        var cached = _edgeFactorCache.Get(edgeTypeId);
        if (cached != null) return cached.Value;

        var factor = _profile.Factor(_edgeTypes.GetById(edgeTypeId));
        _edgeFactorCache.Set(edgeTypeId, factor);

        return factor;
    }

    private TurnCostFactor GetTurnCostFactor(uint turnCostType)
    {
        var cached = _turnCostFactorCache.Get(turnCostType);
        if (cached != null) return cached.Value;

        // the attributes are fetched on a miss rather than carried along every turn cost: there
        // are as many misses as there are turn cost types.
        var factor = _profile.TurnCostFactor(_turnCostTypes.GetById(turnCostType));
        _turnCostFactorCache.Set(turnCostType, factor);

        return factor;
    }

    /// <summary>
    /// Gets the factor for the current edge, in the edge's own direction.
    /// </summary>
    /// <remarks>
    /// The same memo the search uses: route materialisation needs the speed, and a second way to
    /// get a factor is a second way to miss the memo.
    /// </remarks>
    internal EdgeFactor GetDirectedFactor(IEdgeEnumerator<RoutingNetwork> edgeEnumerator)
    {
        var factor = this.GetFactor(edgeEnumerator.EdgeTypeId);

        return edgeEnumerator.Forward ? factor : factor.Reverse;
    }

    public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
        IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
        PreviousEdgeEnumerable previousEdges = default)
    {
        var factor = this.GetDirectedFactor(edgeEnumerator);

        var lengthNullable = edgeEnumerator.Length;
        var length = lengthNullable ??
                     (uint)(edgeEnumerator.EdgeLength() * 100);
        var cost = tailToHead ? factor.ForwardFactor * length : factor.BackwardFactor * length;
        var canAccess = tailToHead ? factor.ForwardFactor > 0 : factor.BackwardFactor > 0;
        var localAccess = factor.IsLocalAccess;

        var totalTurnCost = 0.0;
        var turn = previousEdges.First?.turn;
        if (turn == null) return (canAccess, factor.CanStop, localAccess, cost, totalTurnCost);

        var turnCosts = tailToHead
            ? edgeEnumerator.GetTurnCostToTail(turn.Value)
            : edgeEnumerator.GetTurnCostFromTail(turn.Value);

        foreach (var (turnCostType, turnCost, _) in turnCosts)
        {
            // TODO: compare prefix edges with the previous edges.
            var turnCostFactor = this.GetTurnCostFactor(turnCostType);

            if (turnCostFactor.IsBinary && turnCost > 0)
            {
                totalTurnCost = double.MaxValue;
                break;
            }

            totalTurnCost += turnCostFactor.CostFactor * turnCost;
        }

        return (canAccess, factor.CanStop, localAccess, cost, totalTurnCost);
    }
}
