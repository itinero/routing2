using System.Linq;
using Itinero.Network;

namespace Itinero.Snapping
{
    public static class ISnappableExtensions
    {
        /// <summary>
        /// Snap to the given location.
        /// </summary>
        /// <param name="snappable">The snappable.</param>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        public static Result<SnapPoint> To(this ISnappable snappable, (double longitude, double latitude) location)
        {
            return snappable.To(new[] {location}).First();
        }

        /// <summary>
        /// Snaps to the given vertex.
        /// </summary>
        /// <param name="snappable">The snappable.</param>
        /// <param name="vertexId">The vertex to snap to.</param>
        /// <param name="edgeId">The edge to prefer if any.</param>
        /// <returns>The result if any. Snapping will fail if a vertex has no edges.</returns>
        public static Result<SnapPoint> To(this ISnappable snappable, VertexId vertexId, EdgeId? edgeId = null)
        {
            return snappable.To(new (VertexId vertexId, EdgeId? edgeId)[] {(vertexId, edgeId)}).First();
        }
    }
}