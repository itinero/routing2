using Itinero.Network.Mutation;
using Itinero.Network.Search.Islands;
using Itinero.Network.Writer;

namespace Itinero.Network;

public sealed partial class RoutingNetwork
{
    internal readonly RoutingNetworkIslandManager IslandManager;

    RoutingNetworkIslandManager IRoutingNetworkWritable.IslandManager
    {
        get
        {
            return IslandManager;
        }
    }

    RoutingNetworkIslandManager IRoutingNetworkMutable.IslandManager
    {
        get
        {
            return IslandManager;
        }
    }
}
