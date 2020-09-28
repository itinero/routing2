using System.Linq;
using Itinero.Network;

namespace Itinero.Snapping
{
    public static class ISnapperExtensions
    {
        /// <summary>
        /// Snaps to the given vertex.
        /// </summary>
        /// <param name="snapper">The snapper.</param>
        /// <param name="vertexId">The vertex to snap to.</param>
        /// <param name="edgeId">The edge to prefer if any.</param>
        /// <returns>The result if any. Snapping will fail if a vertex has no edges.</returns>
        public static Result<SnapPoint> To(this ISnapper snapper, VertexId vertexId, EdgeId? edgeId = null)
        {
            return snapper.To(new [] {(vertexId, edgeId)}).First();
        }
    }
}