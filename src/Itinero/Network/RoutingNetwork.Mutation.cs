using System;
using Itinero.Network.DataStructures;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles;

namespace Itinero.Network;

public sealed partial class RoutingNetwork
{
    private readonly object _mutatorSync = new();
    private RoutingNetworkMutator? _graphMutator;

    /// <summary>
    /// Gets a mutable version of this network.
    /// </summary>
    /// <returns>The mutable version.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public RoutingNetworkMutator GetAsMutable()
    {
        lock (_mutatorSync)
        {
            if (_graphMutator != null) throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");

            _graphMutator = new RoutingNetworkMutator(this);
            this.RouterDb.SetAsMutable(_graphMutator);
            return _graphMutator;
        }
    }

    SparseArray<NetworkTile?> IRoutingNetworkMutable.Tiles => _tiles;

    void IRoutingNetworkMutable.ClearMutator()
    {
        _graphMutator = null;
    }
}
