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
        return globalNetworkRestriction.TryBuildNetworkRestriction(
            (geid, _) => getEdge(geid), out networkRestriction);
    }

    /// <summary>
    /// Tries to build a network restriction from a global restriction, passing the pivot
    /// node index (the node shared with the adjacent edge in the restriction chain) to the
    /// resolver so subsection lookups can prefer edges adjacent to that pivot.
    /// </summary>
    public static bool TryBuildNetworkRestriction(this GlobalRestriction globalNetworkRestriction,
        Func<GlobalEdgeId, ushort?, (EdgeId edge, bool forward)?> getEdge,
        [MaybeNullWhen(false)] out NetworkRestriction? networkRestriction)
    {
        networkRestriction = null;

        var edges = new List<(EdgeId edge, bool forward)>();
        for (var i = 0; i < globalNetworkRestriction.Count; i++)
        {
            var globalId = globalNetworkRestriction[i];
            var pivot = ComputePivot(globalNetworkRestriction, i);

            var e = getEdge(globalId, pivot);
            if (e == null) return false;

            edges.Add(e.Value);
        }

        networkRestriction = new NetworkRestriction(edges, globalNetworkRestriction.IsProhibitory,
            globalNetworkRestriction.Attributes);
        return true;
    }

    private static ushort? ComputePivot(GlobalRestriction restriction, int i)
    {
        if (restriction.Count <= 1) return null;

        var current = restriction[i];
        var neighbor = i == 0 ? restriction[i + 1] : restriction[i - 1];

        if (current.EdgeId != neighbor.EdgeId) return null;

        if (current.Tail == neighbor.Tail || current.Tail == neighbor.Head) return current.Tail;
        if (current.Head == neighbor.Tail || current.Head == neighbor.Head) return current.Head;
        return null;
    }
}
