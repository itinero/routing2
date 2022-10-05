using System;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;

namespace Itinero.Snapping;

public static class ILocationsSnapperExtensions
{
    /// <summary>
    /// Snap to the given location.
    /// </summary>
    /// <param name="locationsSnapper">The snappable.</param>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <returns></returns>
    public static async Task<Result<SnapPoint>> ToAsync(this ILocationsSnapper locationsSnapper, double longitude, double latitude)
    {
        var result = locationsSnapper.ToAsync(new[] { (longitude, latitude, (float?)null) });
        var enumerator = result.GetAsyncEnumerator();
        if (!await enumerator.MoveNextAsync()) throw new Exception("There should be one item");

        return enumerator.Current;
    }

    /// <summary>
    /// Snap to the given location.
    /// </summary>
    /// <param name="locationsSnapper">The snappable.</param>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public static async Task<Result<SnapPoint>> ToAsync(this ILocationsSnapper locationsSnapper,
        (double longitude, double latitude, float? e) location)
    {
        var result = locationsSnapper.ToAsync(new[] { location });
        var enumerator = result.GetAsyncEnumerator();
        if (!await enumerator.MoveNextAsync()) throw new Exception("There should be one item");

        return enumerator.Current;
    }
}
