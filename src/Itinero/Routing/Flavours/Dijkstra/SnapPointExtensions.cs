using Itinero.Snapping;

namespace Itinero.Routing.Flavours.Dijkstra
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