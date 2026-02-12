using System.Collections.Generic;
using OsmSharp;

namespace Itinero.IO.Osm.Restrictions.Barriers;

/// <summary>
/// Represents an OSM barrier.
/// </summary>
public class OsmBarrier
{
    private OsmBarrier(Node node, IEnumerable<Way> ways)
    {
        this.Node = node;
        this.Ways = ways;
    }

    /// <summary>
    /// The node where the barrier exists.
    /// </summary>
    public Node Node { get; }
    
    /// <summary>
    /// The way(s).
    /// </summary>
    public IEnumerable<Way> Ways { get; private set; }

    /// <summary>
    /// Creates a new barrier.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="ways">The ways that contain the node.</param>
    /// <returns>The barrier.</returns>
    public static OsmBarrier Create(Node node, IEnumerable<Way> ways)
    {
        return new OsmBarrier(node, ways);
    }
}
