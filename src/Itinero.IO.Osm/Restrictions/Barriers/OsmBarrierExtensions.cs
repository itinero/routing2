using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Tiles.Standalone.Global;

namespace Itinero.IO.Osm.Restrictions.Barriers;

/// <summary>
/// Extension methods for OsmBarrier.
/// </summary>
public static class OsmBarrierExtensions
{
    /// <summary>
    /// Converts the given barrier into two or more global network restrictions.
    /// </summary>
    /// <param name="osmBarrier">The OSM barrier.</param>
    /// <returns>The restrictions using network edges and vertices.</returns>
    public static IEnumerable<GlobalRestriction> ToGlobalNetworkRestrictions(
        this OsmBarrier osmBarrier)
    {
        var attributes = osmBarrier.Node.Tags?.Select(tag => (tag.Key, tag.Value)).ToArray() ??
                         ArraySegment<(string key, string value)>.Empty;
        
        foreach (var tailHop in osmBarrier.GetTailHops())
        foreach (var otherHop in osmBarrier.GetTailHops())
        {
            if (tailHop == otherHop) continue;

            var headHop = otherHop.GetInverted();

            yield return new GlobalRestriction([tailHop, headHop], true, attributes);
        }
    }

    private static IEnumerable<GlobalEdgeId> GetTailHops(
        this OsmBarrier osmBarrier)
    {
        var node = osmBarrier.Node.Id!.Value;

        foreach (var fromWay in osmBarrier.Ways)
        {
            var previous = 0;
            for (var n = 1; n < fromWay.Nodes.Length; n++)
            {
                var current = fromWay.Nodes[n];
                if (current != node) continue;
                if (n == previous) continue;

                yield return GlobalEdgeId.Create(fromWay.Id!.Value, tail: previous, head: n);
                previous = n;
            }
            
            previous = fromWay.Nodes.Length - 1;
            for (var n = fromWay.Nodes.Length - 2; n >= 0; n--)
            {
                var current = fromWay.Nodes[n];
                if (current != node) continue;
                if (n == previous) continue;
                    
                yield return GlobalEdgeId.Create(fromWay.Id!.Value, tail: previous, head: n);
                previous = n;
            }
        }
    }
}
