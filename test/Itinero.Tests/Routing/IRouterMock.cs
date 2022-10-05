using Itinero.Network;
using Itinero.Routing;

namespace Itinero.Tests.Routing
{
    public class IRouterMock : IRouter
    {
        public RoutingNetwork Network { get; init; }
        public RoutingSettings Settings { get; init; }
    }
}
