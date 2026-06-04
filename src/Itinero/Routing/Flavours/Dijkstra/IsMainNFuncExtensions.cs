using Itinero.Network;
using Itinero.Profiles;

namespace Itinero.Routing.Flavours.Dijkstra;

internal static class IsMainNFuncExtensions
{
    /// <summary>
    /// Binds <see cref="RoutingNetworkIslandManager.IsMainN"/> to the given profile, producing
    /// a closure the edge-based Dijkstra can call without knowing about the manager.
    /// </summary>
    public static IsMainNFunc GetIsMainNFunc(this RoutingNetwork network, Profile profile)
    {
        var manager = network.IslandManager;
        return (edgeId, isLocalAccess) => manager.IsMainN(profile, edgeId, isLocalAccess);
    }
}
