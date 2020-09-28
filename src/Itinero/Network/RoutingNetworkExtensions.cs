using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Profiles;
using Itinero.Routing.Costs;

namespace Itinero.Network
{
    internal static class RoutingNetworkSnapshotExtensions
    {
        /// <summary>
        /// Gets the location of the given vertex.
        /// </summary>
        /// <param name="routingNetwork">The network</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>The location.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the vertex doesn't exist.</exception>
        public static (double longitude, double latitude) GetVertex(this RoutingNetwork routingNetwork, VertexId vertex)
        {
            if (!routingNetwork.TryGetVertex(vertex, out var longitude, out var latitude)) throw new ArgumentOutOfRangeException(nameof(vertex));

            return (longitude, latitude);
        }

        internal static ICostFunction GetCostFunctionFor(this RoutingNetwork network, Profile profile)
        {
            if (!network.RouterDb.ProfileConfiguration.TryGetProfileHandlerEdgeTypesCache(profile, out var cache) ||
                cache == null)
            {
                return new ProfileCostFunction(profile);
            }

            return new ProfileCostFunctionCached(profile, cache);
        }

        internal static IEnumerable<(string key, string value)> GetAttributes(this RoutingNetwork routerDb, EdgeId edge)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            if (!enumerator.MoveToEdge(edge)) return Enumerable.Empty<(string key, string value)>();

            return enumerator.Attributes;
        }
    }
}