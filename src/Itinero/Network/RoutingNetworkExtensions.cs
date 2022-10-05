using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Itinero.Routing.Costs.Caches;

[assembly: InternalsVisibleTo("Itinero.Geo")]

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
        public static (double longitude, double latitude, float? e) GetVertex(this RoutingNetwork routingNetwork,
            VertexId vertex)
        {
            if (!routingNetwork.TryGetVertex(vertex, out var longitude, out var latitude, out var e)) {
                throw new ArgumentOutOfRangeException(nameof(vertex));
            }

            return (longitude, latitude, e);
        }

        /// <summary>
        /// Gets an enumerable with all vertices.
        /// </summary>
        /// <param name="routingNetwork">The routing network.</param>
        /// <returns>An enumerable with all vertices.</returns>
        public static IEnumerable<VertexId> GetVertices(this RoutingNetwork routingNetwork)
        {
            var enumerator = routingNetwork.GetVertexEnumerator();
            while (enumerator.MoveNext()) {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        /// Gets an enumerable with all edges.
        /// </summary>
        /// <param name="routingNetwork">The routing network.</param>
        /// <returns>An enumerable with all edges.</returns>
        public static IEnumerable<EdgeId> GetEdges(this RoutingNetwork routingNetwork)
        {
            var edgeEnumerator = routingNetwork.GetEdgeEnumerator();
            foreach (var vertex in routingNetwork.GetVertices()) {
                if (!edgeEnumerator.MoveTo(vertex)) {
                    continue;
                }

                while (edgeEnumerator.MoveNext()) {
                    if (!edgeEnumerator.Forward) {
                        continue;
                    }

                    yield return edgeEnumerator.EdgeId;
                }
            }
        }

        internal static ICostFunction GetCostFunctionFor(this RoutingNetwork network, Profile profile)
        {
            if (!network.RouterDb.ProfileConfiguration.TryGetProfileHandlerEdgeTypesCache(profile, out var cache, out var turnCostFactorCache)) {
                return new ProfileCostFunction(profile);
            }

            return new ProfileCostFunctionCached(profile, cache ?? new EdgeFactorCache(), turnCostFactorCache ?? new TurnCostFactorCache());
        }

        internal static IEnumerable<(string key, string value)>
            GetAttributes(this RoutingNetwork routerDb, EdgeId edge)
        {
            var enumerator = routerDb.GetEdgeEnumerator();
            if (!enumerator.MoveToEdge(edge)) {
                return Enumerable.Empty<(string key, string value)>();
            }
        
            return enumerator.Attributes;
        }
    }
}