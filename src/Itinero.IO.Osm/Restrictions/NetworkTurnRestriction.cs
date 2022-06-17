using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Network;

[assembly: InternalsVisibleTo("Itinero.Tests")]

namespace Itinero.IO.Osm.Restrictions;

/// <summary>
/// A turn restriction on the network.
/// </summary>
public class NetworkTurnRestriction : IEnumerable<(EdgeId edge, bool forward)>
{
    private readonly List<(EdgeId edge, bool forward)> _sequence;

    /// <summary>
    /// Creates a new network turn restriction.
    /// </summary>
    /// <param name="sequence">The sequence that is either prohibited or mandatory.</param>
    /// <param name="isProhibitory">Flag to set the restriction to prohibited or mandatory.</param>
    /// <param name="attributes">The attributes.</param>
    public NetworkTurnRestriction(IEnumerable<(EdgeId edge, bool forward)> sequence, bool isProhibitory,
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