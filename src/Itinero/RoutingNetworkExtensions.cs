using Itinero.Network;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero
{
    public static class RoutingNetworkExtensions
    {
        public static ISnapper Snap(this RoutingNetwork routingNetwork)
        {
            return new Snapper(routingNetwork);
        }

        public static IRouter Route(this RoutingNetwork routingNetwork, RoutingSettings? settings = null)
        {
            return new Router(routingNetwork, settings ??= new RoutingSettings());
        }
    }
}