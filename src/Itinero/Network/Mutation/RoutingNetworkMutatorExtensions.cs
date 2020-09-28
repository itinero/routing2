using System;

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
        public static (double longitude, double latitude) GetVertex(this RoutingNetworkMutator routingNetwork, VertexId vertex)
        {
            if (!routingNetwork.TryGetVertex(vertex, out var longitude, out var latitude)) throw new ArgumentOutOfRangeException(nameof(vertex));

            return (longitude, latitude);
        }

        public static VertexId AddVertex(this RoutingNetworkMutator routingNetwork, (double longitude, double latitude) location)
        {
            return routingNetwork.AddVertex(location.longitude, location.latitude);
        }
    }
}