using System;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero.Algorithms.Dijkstra
{
    internal static class SnapPointExtensions
    {
        /// <summary>
        /// Returns a factor in the range [0, 1] representing the position on the edge.
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <returns>The factor.</returns>
        internal static double OffsetFactor(this SnapPoint snapPoint)
        {
            return snapPoint.Offset / (double) ushort.MaxValue;
        }
    }
}