using System;
using System.Collections.Generic;
using Itinero.Network;
using NetTopologySuite.Features;

namespace Itinero.Geo
{
    public static class RoutingNetworkExtensions
    {
        /// <summary>
        /// Converts the RoutingNetwork into a stream of GeoFeatures
        /// </summary>
        /// <param name="routerDb">The network to convert</param>
        public static IEnumerable<IFeature> AsStream(this RouterDb routerDb)
        {
            return routerDb.Latest.AsStream();
        }

        /// <summary>
        /// Converts the RoutingNetwork into a stream of GeoFeatures
        /// </summary>
        /// <param name="network">The network to convert</param>
        public static IEnumerable<IFeature> AsStream(this RoutingNetwork network)
        {
            return new RoutingNetworkStream(network, null);
        }


        /// <summary>
        /// Converts the RoutingNetwork into a stream of GeoFeatures
        /// </summary>
        /// <param name="routerDb">The network to convert</param>
        /// <param name="preprocessAttributes">The function which preprocesses the attributes on the edges</param>
        public static IEnumerable<IFeature> AsStream(this RouterDb routerDb,
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> preprocessAttributes)
        {
            return routerDb.Latest.AsStream(preprocessAttributes);
        }

        /// <summary>
        /// Converts the RoutingNetwork into a stream of GeoFeatures
        /// </summary>
        /// <param name="network">The network to convert</param>
        /// <param name="preprocessAttributes">The function which preprocesses the attributes on the edges</param>
        public static IEnumerable<IFeature> AsStream(this RoutingNetwork network,
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>> preprocessAttributes)
        {
            return new RoutingNetworkStream(network, preprocessAttributes);
        }
    }
}