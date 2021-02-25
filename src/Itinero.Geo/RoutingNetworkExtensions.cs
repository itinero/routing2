using System.Collections.Generic;
using Itinero.Network;
using NetTopologySuite.Features;

namespace Itinero.Geo
{
    public static class RoutingNetworkExtensions
    {

        public static IEnumerable<IFeature> AsStream(this RouterDb routerDb)
        {
            return routerDb.Latest.AsStream();
        }
        
        public static IEnumerable<IFeature> AsStream(this RoutingNetwork network)
        {
            return new RoutingNetworkStream(network);
        }
    }
}