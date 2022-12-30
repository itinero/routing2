using System;
using Itinero.Network.DataStructures;
using Itinero.Network.Mutation;
using Itinero.Network.Tiles;

namespace Itinero.Network;

public sealed partial class RoutingNetwork
{
    private readonly object _mutatorSync = new();
    private RoutingNetworkMutator? _graphMutator;

    internal RoutingNetworkMutator GetAsMutable()
    {
        lock (_mutatorSync)
        {
            if (_graphMutator != null) throw new InvalidOperationException($"Only one mutable graph is allowed at one time.");

            _graphMutator = new RoutingNetworkMutator(this);
            return _graphMutator;
        }
    }

    SparseArray<NetworkTile?> IRoutingNetworkMutable.Tiles => _tiles;

    void IRoutingNetworkMutable.ClearMutator()
    {
        _graphMutator = null;
    }
}
