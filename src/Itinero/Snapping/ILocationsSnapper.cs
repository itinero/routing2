using System.Collections.Generic;

namespace Itinero.Snapping
{
    /// <summary>
    /// A snapper end point accepting locations.
    /// </summary>
    public interface ILocationsSnapper
    {
        /// <summary>
        /// Snaps to the given locations.
        /// </summary>
        /// <param name="locations">The locations.</param>
        /// <returns>The snapped locations.</returns>
        IAsyncEnumerable<Result<SnapPoint>> ToAsync(IEnumerable<(double longitude, double latitude, float? e)> locations);
    }
}