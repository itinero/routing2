using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;

namespace Itinero.Snapping;

/// <summary>
/// Contains extensions for ILocations
/// </summary>
public static class ISnapperExtensions
{
    /// <summary>
    /// Snap to the given location.
    /// </summary>
    /// <param name="snapper">The snappable.</param>
    /// <param name="location">The location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static async Task<Result<SnapPoint>> ToAsync(this ISnapper snapper,
        (double longitude, double latitude, float? e) location, CancellationToken cancellationToken = default)
    {
        return await snapper.ToAsync(location.longitude, location.latitude, cancellationToken);
    }

    /// <summary>
    /// Snaps to the given locations.
    /// </summary>
    /// <param name="snapper"></param>
    /// <param name="locations"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static async IAsyncEnumerable<Result<SnapPoint>> ToAsync(this ISnapper snapper, IEnumerable<(double longitude, double latitude, float? e)> locations,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var location in locations)
        {
            yield return await snapper.ToAsync(location, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Snap to the given location.
    /// </summary>
    /// <param name="snapper">The snappable.</param>
    /// <param name="location">The location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static async Task<Result<VertexId>> ToVertexAsync(this ISnapper snapper,
        (double longitude, double latitude, float? e) location, CancellationToken cancellationToken = default)
    {
        return await snapper.ToVertexAsync(location.longitude, location.latitude, cancellationToken);
    }

    /// <summary>
    /// Snaps to the given locations.
    /// </summary>
    /// <param name="snapper">The locations snapper.</param>
    /// <param name="locations">The locations.</param>
    /// <returns>The vertices snapped to.</returns>
    public static async IAsyncEnumerable<Result<VertexId>> ToVerticesAsync(this ISnapper snapper, IEnumerable<(double longitude, double latitude, float? e)> locations)
    {
        foreach (var location in locations)
        {
            yield return await snapper.ToVertexAsync(location);
        }
    }

    /// <summary>
    /// Snaps to the given vertex and edge exactly.
    /// </summary>
    /// <param name="snapper">The snapper.</param>
    /// <param name="vertexId">The vertex to snap to.</param>
    /// <param name="edgeId">The edge.</param>
    /// <param name="asDeparture">When this has a value, any edge will be checked against the configured profile(s) as suitable for departure at the given vertex, when true, or arrival, when false.</param>
    /// <returns>The result if any. Snapping will fail if a vertex has no edges.</returns>
    public static Result<SnapPoint> ToExact(this ISnapper snapper, VertexId vertexId, EdgeId edgeId, bool asDeparture = true)
    {
        var result = snapper.To(vertexId, asDeparture);
        foreach (var result1 in result)
        {
            if (result1.Value.EdgeId == edgeId) return result1;
        }
        return new Result<SnapPoint>("Vertex probably does not have edge as neighbour or edge id not snappable by configured profiles");
    }
}
