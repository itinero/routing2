using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Network.Tiles.Standalone.Global;

/// <summary>
/// A global restriction.
///
/// This contains:
/// - The sequence of edges that incurs the cost.
/// - A prohibitory flag:
///   - true : only this specific turn with the prefix edges is forbidden,
///   - false: only this turn is allowed, all other turns at the vertex are forbidden.
/// 
/// Assumptions:
/// - The restriction occurs at the vertex between the last two edges in the sequence.
/// </summary>
/// <remarks>
/// Why model restrictions like this:
/// We do not know the full structure of the network at the time this is defined only the part within the tile.
/// </remarks>
public class GlobalRestriction : IReadOnlyList<GlobalEdgeId>
{
    private readonly IReadOnlyList<GlobalEdgeId> _edges;

    /// <summary>
    /// Creates a new network restriction.
    /// </summary>
    /// <param name="sequence">The sequence that is either prohibited or mandatory.</param>
    /// <param name="isProhibitory">Flag to set the restriction to prohibited or mandatory.</param>
    /// <param name="attributes">The attributes.</param>
    public GlobalRestriction(IEnumerable<GlobalEdgeId> sequence, bool isProhibitory,
        IEnumerable<(string key, string value)> attributes)
    {
        _edges = sequence.ToList();
        if (_edges.Count < 2) throw new ArgumentException("A restriction has to have at least 2 edges");

        this.IsProhibitory = isProhibitory;
        this.Attributes = attributes;
    }

    /// <summary>
    /// Returns true if the restriction is negative.
    /// </summary>
    public bool IsProhibitory { get; }

    /// <summary>
    /// The attributes associated with the restriction.
    /// </summary>
    public IEnumerable<(string key, string value)> Attributes { get; }

    public IEnumerator<GlobalEdgeId> GetEnumerator()
    {
        return _edges.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_edges).GetEnumerator();
    }

    public int Count => _edges.Count;

    public GlobalEdgeId this[int index] => _edges[index];
}
