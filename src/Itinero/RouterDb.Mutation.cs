using System;
using Itinero.Data.Providers;
using Itinero.Network;

namespace Itinero;

public sealed partial class RouterDb
{
    // the one-open-transaction guard, and the whole of what publishing needs to track.
    private NetworkBuilder? _open;

    /// <summary>
    /// Opens a transaction against the newest network, to be published when it is disposed.
    /// </summary>
    internal NetworkBuilder GetMutableNetwork()
    {
        if (_open != null)
        {
            throw new InvalidOperationException("Only one open transaction is allowed at a time.");
        }

        _open = new NetworkBuilder(this.Latest);

        return _open;
    }

    /// <summary>
    /// Publishes the network the given transaction wrote, and closes it.
    /// </summary>
    internal void Finish(RoutingNetwork newNetwork)
    {
        this.Latest = newNetwork;

        _open = null;
    }
}
