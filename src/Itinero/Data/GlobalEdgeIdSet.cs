using System;
using System.Collections;
using System.Collections.Generic;
using Itinero.Network;

namespace Itinero.Data;

/// <summary>
/// A default global id set for Guid ids.
/// </summary>
public sealed class GlobalEdgeIdSet : IEnumerable<(Guid globalId, EdgeId edgeId)>
{
    private readonly Dictionary<Guid, EdgeId> _set = new();

    /// <summary>
    /// Sets a new mapping.
    /// </summary>
    /// <param name="globalEdgeId">The global edge id.</param>
    /// <param name="edgeId">The local edge id.</param>
    public void Set(Guid globalEdgeId, EdgeId edgeId)
    {
        _set[globalEdgeId] = edgeId;
    }

    /// <summary>
    /// Removes a mapping.
    /// </summary>
    /// <param name="globalEdgeId">The global edge id.</param>
    public void Remove(Guid globalEdgeId)
    {
        _set.Remove(globalEdgeId);
    }

    /// <summary>
    /// Gets a mapping if it exists.
    /// </summary>
    /// <param name="globalEdgeId">The global edge id.</param>
    /// <param name="edgeId">The edge associated with the given global edge, if any.</param>
    /// <returns>True if a mapping exists, false otherwise.</returns>
    public bool TryGet(Guid globalEdgeId, out EdgeId edgeId)
    {
        return _set.TryGetValue(globalEdgeId, out edgeId);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<(Guid globalId, EdgeId edgeId)> GetEnumerator()
    {
        foreach (var (key, value) in _set)
        {
            yield return (key, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
