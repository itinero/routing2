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
    /// Called when a vertex is touched.
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
