using System.Linq;

namespace Itinero.Snapping
{
    public static class ILocationsSnapperExtensions
    {
        /// <summary>
        /// Snap to the given location.
        /// </summary>
        /// <param name="locationsSnapper">The snappable.</param>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        public static Result<SnapPoint> To(this ILocationsSnapper locationsSnapper, (double longitude, double latitude) location)
        {
            return locationsSnapper.To(new[] {location}).First();
        }
    }
}