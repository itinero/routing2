using System;
using Itinero.Profiles;

namespace Itinero.Network.Mutation
{
    public static class RoutingNetworkMutatorExtensions
    {
        /// <summary>
        /// Gets the location of the given vertex.
        /// </summary>
        /// <param name="routingNetwork">The network</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>The location.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the vertex doesn't exist.</exception>
        public static (double longitude, double latitude, float? e) GetVertex(this RoutingNetworkMutator routingNetwork,
            VertexId vertex)
        {
            if (!routingNetwork.TryGetVertex(vertex, out var longitude, out var latitude, out var e)) {
                throw new ArgumentOutOfRangeException(nameof(vertex));
            }

            return (longitude, latitude, e);
        }

        public static VertexId AddVertex(this RoutingNetworkMutator routingNetwork,
            (double longitude, double latitude, float? e) location)
        {
            return routingNetwork.AddVertex(location.longitude, location.latitude, location.e);
        }

        public static void PrepareFor(this RoutingNetworkMutator routingNetworkMutator, Profile profile) { }
    }
}