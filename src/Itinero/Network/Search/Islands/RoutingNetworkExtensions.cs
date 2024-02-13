using System;

namespace Itinero.Network.Search.Islands;

internal static class RoutingNetworkExtensions
{
    public static IIslandBuilder Islands(this RoutingNetwork network, Action<IslandBuilderSettings>? settings = null)
    {
        var s = new IslandBuilderSettings();
        settings?.Invoke(s);

        var islandBuilder = new IslandBuilder(network, s);
        return islandBuilder;
    }
}
