using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;

namespace Itinero.Snapping;

/// <summary>
/// A snapper end point accepting locations.
/// </summary>
public interface ISnapper
{
    /// <summary>
    /// Snaps to the given locations.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The snapped locations.</returns>
    Task<Result<SnapPoint>> ToAsync(double longitude, double latitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Snaps to all the possible edges nearby.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The locations on edges nearby.</returns>
    IAsyncEnumerable<SnapPoint> ToAllAsync(double longitude, double latitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Snaps to the given location.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The snapped locations.</returns>
    Task<Result<VertexId>> ToVertexAsync(double longitude, double latitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Snaps to all the possible vertices nearby.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The vertices nearby.</returns>
    IAsyncEnumerable<VertexId> ToAllVerticesAsync(double longitude, double latitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Snaps to the given vertex.
    /// </summary>
    /// <param name="vertexId">The vertex.</param>
    /// <param name="asDeparture">When this has a value, any edge will be checked against the configured profile(s) as suitable for departure at the given vertex, when true, or arrival, when false.</param>
    /// <returns>The results if any. Snapping will fail if a vertex has no edges or cannot be accessed by any configured profiles.</returns>
    IEnumerable<Result<SnapPoint>> To(VertexId vertexId, bool asDeparture = true);

    /// <summary>
    /// Snaps to the given vertex.
    /// </summary>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="offset">The vertex.</param>
    /// <param name="forward">When this has a value, any edge will be checked against the configured profile(s) as suitable for departure, when true, or arrival, when false.</param>
    /// <returns>The results if any. If the edge cannot be accessed by any configured profiles.</returns>
    Result<SnapPoint> To(EdgeId edgeId, ushort offset, bool forward = true);
}
