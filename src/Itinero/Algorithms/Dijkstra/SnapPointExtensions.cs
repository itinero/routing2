using System;
using Itinero.Data.Graphs;
using Itinero.Profiles;

namespace Itinero.Algorithms.Dijkstra
{
    public static class SnapPointExtensions
    {
        /// <summary>
        /// Returns a factor in the range [0, 1] representing the position on the edge.
        /// </summary>
        /// <param name="snapPoint">The snap point.</param>
        /// <returns>The factor.</returns>
        internal static float OffsetFactor(this SnapPoint snapPoint)
        {
            return (float) snapPoint.Offset / (float) ushort.MaxValue;
        }
    }
}