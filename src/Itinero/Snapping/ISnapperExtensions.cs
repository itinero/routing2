using System;
using System.Linq;
using System.Threading.Tasks;
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
        public static async Task<Result<SnapPoint>> ToAsync(this ISnapper snapper, VertexId vertexId, EdgeId? edgeId = null)
        {
            var result = snapper.ToAsync(new[] { (vertexId, edgeId) });
            var enumerator = result.GetAsyncEnumerator();
            if (!await enumerator.MoveNextAsync()) throw new Exception("There should be one item");

            return enumerator.Current;
        }

    }
}
