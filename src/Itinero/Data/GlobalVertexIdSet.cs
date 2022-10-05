using System.Collections;
using System.Collections.Generic;
using Itinero.Network;

namespace Itinero.Data;

/// <summary>
/// Abstract definition of a global id map for vertices.
/// </summary>
public sealed class GlobalVertexIdSet : IEnumerable<(long globalId, VertexId vertex)>
{
    private readonly Dictionary<long, VertexId> _set = new();

    /// <summary>
    /// Sets a new mapping.
    /// </summary>
    /// <param name="globalVertexId">The global vertex id.</param>
    /// <param name="vertex">The local vertex.</param>
    public void Set(long globalVertexId, VertexId vertex)
    {
        _set[globalVertexId] = vertex;
    }

    /// <summary>
    /// Removes a mapping.
    /// </summary>
    /// <param name="globalVertexId">The global vertex id.</param>
    public void Remove(long globalVertexId)
    {
        _set.Remove(globalVertexId);
    }

    /// <summary>
    /// Gets a mapping if it exists.
    /// </summary>
    /// <param name="globalVertexId">The global vertex id.</param>
    /// <param name="vertex">The vertex associated with the given global vertex, if any.</param>
    /// <returns>True if a mapping exists, false otherwise.</returns>
    public bool TryGet(long globalVertexId, out VertexId vertex)
    {
        return _set.TryGetValue(globalVertexId, out vertex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<(long globalId, VertexId vertex)> GetEnumerator()
    {
        foreach (var (key, value) in _set)
        {
            yield return (key, value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
