using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;

namespace Itinero.Data.Usage;

/// <summary>
/// Notifies listeners about data use in a router db.
/// </summary>
public class DataUseNotifier
{
    private readonly HashSet<IDataUseListener> _listeners = new();

    /// <summary>
    /// Adds a new listener.
    /// </summary>
    /// <param name="listener">The new listener.</param>
    public void AddListener(IDataUseListener listener)
    {
        _listeners.Add(listener);
    }

    internal IEnumerable<IDataUseListener> RegisteredListeners
    {
        get
        {
            return _listeners.ToList();
        }
    }

    /// <summary>
    /// Returns true if all listeners report the vertex data is ready (no async work needed).
    /// </summary>
    internal bool IsVertexDataReady(RoutingNetwork network, VertexId vertex)
    {
        foreach (var listener in _listeners)
        {
            if (!listener.IsVertexDataReady(network, vertex))
            {
                return false;
            }
        }

        return true;
    }

    internal async Task NotifyVertex(RoutingNetwork network, VertexId vertex, CancellationToken cancellationToken = default)
    {
        foreach (var listener in _listeners)
        {
            await listener.VertexTouched(network, vertex, cancellationToken);
        }
    }

    internal async Task NotifyBox(RoutingNetwork network,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) box, CancellationToken cancellationToken = default)
    {
        foreach (var listener in _listeners)
        {
            await listener.BoxTouched(network, box, cancellationToken);
        }
    }
}
