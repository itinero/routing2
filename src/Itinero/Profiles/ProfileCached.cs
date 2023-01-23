using System.Collections.Generic;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs.Caches;

namespace Itinero.Profiles;

/// <summary>
/// A cached version of a profile.
/// </summary>
public class ProfileCached
{
    private readonly Profile _profile;
    private readonly EdgeFactorCache _edgeFactorCache;
    private readonly TurnCostFactorCache _turnCostFactorCache;

    internal ProfileCached(Profile profile, EdgeFactorCache edgeFactorCache, TurnCostFactorCache turnCostFactorCache)
    {
        _profile = profile;
        _edgeFactorCache = edgeFactorCache;
        _turnCostFactorCache = turnCostFactorCache;
    }

    /// <summary>
    /// Gets an edge factor for the current edge.
    /// </summary>
    /// <param name="edgeEnumerator">The enumerator with the current edge.</param>
    /// <returns>The edge factor.</returns>
    public EdgeFactor Factor(RoutingNetworkEdgeEnumerator edgeEnumerator)
    {
        // get edge factor and length.
        EdgeFactor factor;
        var edgeTypeId = edgeEnumerator.EdgeTypeId;
        if (edgeTypeId == null)
        {
            factor = _profile.FactorInEdgeDirection(edgeEnumerator);
        }
        else
        {
            var edgeFactor = _edgeFactorCache.Get(edgeTypeId.Value);
            if (edgeFactor == null)
            {
                // no cached value, cache forward value.
                factor = _profile.Factor(edgeEnumerator.Attributes);
                _edgeFactorCache.Set(edgeTypeId.Value, factor);
            }
            else
            {
                factor = edgeFactor.Value;
            }

            // cached value is always forward.
            if (!edgeEnumerator.Forward)
            {
                factor = factor.Reverse;
            }
        }

        return factor;
    }
}
