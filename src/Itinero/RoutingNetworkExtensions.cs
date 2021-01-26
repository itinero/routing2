using Itinero.Network;
using Itinero.Profiles;
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

        public static IRouter Route(this RoutingNetwork routingNetwork, Profile profile)
        {
            return new Router(routingNetwork, new RoutingSettings {
                Profile = profile
            });
        }

        public static IRouter Route(this RoutingNetwork routingNetwork, RoutingSettings settings)
        {
            return new Router(routingNetwork, settings);
        }
    }
}