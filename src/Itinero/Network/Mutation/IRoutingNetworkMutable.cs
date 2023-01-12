using Itinero.Data.Usage;
using Itinero.Network.DataStructures;
using Itinero.Network.Tiles;

namespace Itinero.Network.Mutation;

internal interface IRoutingNetworkMutable
{
    int Zoom { get; }

    RouterDb RouterDb { get; }

    SparseArray<NetworkTile?> Tiles { get; }

    DataUseNotifier UsageNotifier { get; }

    void ClearMutator();
}
