using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Network;

[assembly: InternalsVisibleTo("Itinero.Tests")]

namespace Itinero.IO.Osm.Restrictions;

/// <summary>
/// A restriction on the network.
/// </summary>
/// <remarks>
/// A sequence of restricted edges that can either be prohibitive or obligatory. This can be used to represent the classic turn restrictions but also barriers.
/// </remarks>
public class NetworkRestriction : IEnumerable<(EdgeId edge, bool forward)>
{
    private readonly List<(EdgeId edge, bool forward)> _sequence;

    /// <summary>
    /// Creates a new network turn restriction.
    /// </summary>
    /// <param name="sequence">The sequence that is either prohibited or mandatory.</param>
    /// <param name="isProhibitory">Flag to set the restriction to prohibited or mandatory.</param>
    /// <param name="attributes">The attributes.</param>
    public NetworkRestriction(IEnumerable<(EdgeId edge, bool forward)> sequence, bool isProhibitory,
        IEnumerable<(string key, string value)> attributes)
    {
        _sequence = new List<(EdgeId edge, bool forward)>(sequence);
        if (_sequence.Count < 2) throw new ArgumentException("A restriction has to have at least 2 edges");

        this.IsProhibitory = isProhibitory;
        this.Attributes = attributes;
    }

    /// <summary>
    /// Gets the number of edges.
    /// </summary>
    public int Count => _sequence.Count;

    /// <summary>
    /// Gets the edge at the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    public (EdgeId edge, bool forward) this[int index] => _sequence[index];

    /// <summary>
    /// Returns true if the restriction is negative.
    /// </summary>
    public bool IsProhibitory { get; }

    /// <summary>
    /// The attributes associated with the restriction.
    /// </summary>
    public IEnumerable<(string key, string value)> Attributes { get; }

    public IEnumerator<(EdgeId edge, bool forward)> GetEnumerator()
    {
        return _sequence.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
