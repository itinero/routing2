using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Geo;

namespace Itinero.Network.Enumerators.Edges
{
    public static class RoutingNetworkEdgeEnumeratorExtensions
    {
        /// <summary>
        /// Gets the complete shape, including start end end vertices.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>The complete shape.</returns>
        public static IEnumerable<(double longitude, double latitude)> GetCompleteShape(
            this RoutingNetworkEdgeEnumerator enumerator)
        {
            return enumerator.GetCompleteShape<RoutingNetworkEdgeEnumerator, RoutingNetwork>();
        }

        /// <summary>
        /// Gets the length of an edge in centimeters.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>The length in meters.</returns>
        internal static double EdgeLength(this RoutingNetworkEdgeEnumerator enumerator)
        {
            return enumerator.EdgeLength<RoutingNetworkEdgeEnumerator, RoutingNetwork>();
        }

        /// <summary>
        /// Returns the part of the edge between the two offsets not including the vertices at the start or the end.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset1">The start offset.</param>
        /// <param name="offset2">The end offset.</param>
        /// <param name="includeVertices">Include vertices in case the range start at min offset or ends at max.</param>
        /// <returns>The shape points between the given offsets. Includes the vertices by default when offsets at min/max.</returns>
        internal static IEnumerable<(double longitude, double latitude)> GetShapeBetween(this RoutingNetworkEdgeEnumerator enumerator,
            ushort offset1 = 0, ushort offset2 = ushort.MaxValue, bool includeVertices = true)
        {
            return enumerator.GetShapeBetween<RoutingNetworkEdgeEnumerator, RoutingNetwork>(offset1, offset2, includeVertices);
        }

        /// <summary>
        /// Returns the location on the given edge using the given offset.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>The location on the network.</returns>
        internal static (double longitude, double latitude) LocationOnEdge(this RoutingNetworkEdgeEnumerator enumerator, in ushort offset)
        {
            return enumerator.LocationOnEdge<RoutingNetworkEdgeEnumerator, RoutingNetwork>(offset);
        }
    }
}