using Itinero.Network;

namespace Itinero.Snapping
{
    internal class Snapper : ISnapper
    {
        private readonly RoutingNetwork _routingNetwork;

        public Snapper(RoutingNetwork routingNetwork)
        {
            _routingNetwork = routingNetwork;
        }
        
        
    }
}