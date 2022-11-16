using System.Collections.Generic;
using System.Threading;

namespace Itinero.Snapping;

/// <summary>
/// A snapper end point accepting locations.
/// </summary>
public interface ILocationsSnapper
{
    /// <summary>
    /// Snaps to the given locations.
    /// </summary>
    /// <param name="locations">The locations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The snapped locations.</returns>
    IAsyncEnumerable<Result<SnapPoint>> ToAsync(IEnumerable<(double longitude, double latitude, float? e)> locations,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Snaps to all the possible edges nearby.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The locations nearby.</returns>
    IAsyncEnumerable<SnapPoint> ToAllAsync((double longitude, double latitude, float? e) location,
        CancellationToken cancellationToken = default);
}
