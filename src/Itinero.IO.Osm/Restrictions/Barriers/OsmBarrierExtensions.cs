using System;
using System.Collections.Generic;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.IO.Osm.Restrictions.Barriers;

/// <summary>
/// Extension methods for OsmBarrier.
/// </summary>
public static class OsmBarrierExtensions
{
    /// <summary>
    /// The signature of a function to get edges for the given nodes pair along the given way.
    /// </summary>
    public delegate IEdgeEnumerator GetEdgesFor(long node);

    /// <summary>
    /// Converts the given barrier into one or more network restrictions
    /// </summary>
    /// <param name="osmBarrier">The OSM barrier.</param>
    /// <param name="getEdgesFor">A function to get edges for a given node.</param>
    /// <returns>The restrictions using network edges and vertices.</returns>
    public static Result<IEnumerable<NetworkRestriction>> ToNetworkRestrictions(
        this OsmBarrier osmBarrier,
        GetEdgesFor getEdgesFor)
    {
        // get all edges starting at the given node.
        var edges = new List<(EdgeId edge, bool forward)>();
        var enumerator = getEdgesFor(osmBarrier.Node);
        while (enumerator.MoveNext())
        {
            edges.Add((enumerator.EdgeId, enumerator.Forward));
        }

        if (edges.Count < 2) return new Result<IEnumerable<NetworkRestriction>>(ArraySegment<NetworkRestriction>.Empty);
        
        // for each two edges create one restriction.
        var restrictions = new List<NetworkRestriction>();
        foreach (var from in edges)
        foreach (var to in edges)
        {
            restrictions.Add(new NetworkRestriction(new []{ from, to}, true,osmBarrier.Attributes));
        }

        return new Result<IEnumerable<NetworkRestriction>>(restrictions);
    }
}