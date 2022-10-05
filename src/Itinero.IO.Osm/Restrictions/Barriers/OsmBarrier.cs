using System.Collections.Generic;
using OsmSharp;

namespace Itinero.IO.Osm.Restrictions.Barriers;

/// <summary>
/// Represents an OSM barrier.
/// </summary>
public class OsmBarrier
{
    private OsmBarrier(long node, IEnumerable<(string key, string value)> attributes)
    {
        this.Node = node;
        this.Attributes = attributes;
    }
    
    /// <summary>
    /// The node where the barrier exists at.
    /// </summary>
    public long Node { get; }
    
    /// <summary>
    /// The attributes associated with the barrier.
    /// </summary>
    public IEnumerable<(string key, string value)> Attributes { get; }

    /// <summary>
    /// Creates a new barrier.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="attributes">The attributes.</param>
    /// <returns>The barrier.</returns>
    public static OsmBarrier Create(long node, IEnumerable<(string key, string value)> attributes)
    {
        return new OsmBarrier(node, attributes);
    }
}