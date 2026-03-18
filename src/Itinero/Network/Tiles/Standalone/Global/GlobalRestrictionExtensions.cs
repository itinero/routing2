using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Itinero.Network.Tiles.Standalone.Global;

public static class GlobalRestrictionExtensions
{
    /// <summary>
    /// Tries to build a network restriction from a global restriction.
    /// </summary>
    /// <remarks>
    /// Should always succeed if all edges are available but can fail if the data available is a tile and does not contain all edges yet.
    /// </remarks>
    /// <param name="globalNetworkRestriction">The global network.</param>
    /// <param name="getEdge">The function to get an edge.</param>
    /// <param name="networkRestriction">The resulting network restriction, if any.</param>
    /// <returns>True if success, false otherwise.</returns>
    public static bool TryBuildNetworkRestriction(this GlobalRestriction globalNetworkRestriction, Func<GlobalEdgeId, (EdgeId edge, bool forward)?> getEdge,
        [MaybeNullWhen(false)] out NetworkRestriction? networkRestriction)
    {
        networkRestriction = null;

        var edges = new List<(EdgeId edge, bool forward)>();
        foreach (var globalId in globalNetworkRestriction)
        {
            var e = getEdge(globalId);
            if (e == null) return false;

            edges.Add(e.Value);
        }

        networkRestriction = new NetworkRestriction(edges, globalNetworkRestriction.IsProhibitory,
            globalNetworkRestriction.Attributes);
        return true;
    }
}
