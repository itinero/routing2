using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OsmSharp;

namespace Itinero.IO.Osm.Restrictions.Barriers;

/// <summary>
/// Parses OSM barriers.
/// </summary>
public class OsmBarrierParser
{
    /// <summary>
    /// Returns true if the node is a barrier.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>True if the node is a barrier, false otherwise.</returns>
    public bool IsBarrier(Node node)
    {
        return node.Tags != null && node.Tags.ContainsKey("barrier");
    }

    /// <summary>
    /// Tries to parse a barrier.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="barrier">The barrier, if any.</param>
    /// <returns>True if parsing succeeded.</returns>
    public bool TryParse(Node node, [NotNullWhen(returnValue: true)] out OsmBarrier? barrier)
    {
        if (node.Id == null) throw new ArgumentException("Node with id null cannot be a barrier");
        
        barrier = null;

        if (!this.IsBarrier(node)) return false;

        barrier = OsmBarrier.Create(node.Id.Value, node.Tags.Select(t => (t.Key, t.Value)).ToList());

        return true;
    }
}
