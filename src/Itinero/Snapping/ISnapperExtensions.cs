using System;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;

namespace Itinero.Snapping;

/// <summary>
/// Contains extension methods for snapper.
/// </summary>
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
        var result = snapper.To(new[] { (vertexId, edgeId) });
        using var enumerator = result.GetEnumerator();
        if (!enumerator.MoveNext()) throw new Exception("There should be one item");

        return enumerator.Current ?? throw new Exception("Current cannot be null");
    }
}
