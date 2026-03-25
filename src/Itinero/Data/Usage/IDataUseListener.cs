using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;

namespace Itinero.Data.Usage;

/// <summary>
/// Abstract definition of a data use listener.
/// </summary>
public interface IDataUseListener
{
    /// <summary>
    /// Clones this data use listener for a new network, if possible.
    /// </summary>
    /// <param name="routingNetwork">The routing network.</param>
    /// <returns>A new data use listener or null if not needed or possible.</returns>
    IDataUseListener? CloneForNewNetwork(RoutingNetwork routingNetwork);

    /// <summary>
    /// Returns true if the data for the given vertex is ready and no async loading is needed.
    /// This is the fast path — called synchronously on every vertex during routing.
    /// When this returns true, <see cref="VertexTouched"/> will not be called for this vertex.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="vertex">The vertex being touched.</param>
    /// <returns>True if the vertex data is already available, false if async loading may be needed.</returns>
    bool IsVertexDataReady(RoutingNetwork network, VertexId vertex)
    {
        return false;
    }

    /// <summary>
    /// Called when a vertex is touched and <see cref="IsVertexDataReady"/> returned false.
    /// </summary>
    /// <param name="network">The network.</param>
    /// <param name="vertex">The vertex that was touched.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task VertexTouched(RoutingNetwork network, VertexId vertex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when an area in general is going to be touched.
    /// </summary>
    /// <param name="network"></param>
    /// <param name="box"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task BoxTouched(RoutingNetwork network,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) box, CancellationToken cancellationToken = default);
}
