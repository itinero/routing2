using Itinero.Network;

namespace Itinero.Data;

/// <summary>
/// Abstract definition of a global id map.
/// </summary>
public interface IGlobalIdSet
{
    /// <summary>
    /// Sets the vertex id for the given global id.
    /// </summary>
    /// <param name="globalId">The global id.</param>
    /// <param name="vertexId">The vertex id.</param>
    void Set(long globalId, VertexId vertexId);

    /// <summary>
    /// Gets the vertex if for the given global id.
    /// </summary>
    /// <param name="globalId">The global id.</param>
    /// <param name="vertexId">The vertex id.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool TryGet(long globalId, out VertexId vertexId);
}