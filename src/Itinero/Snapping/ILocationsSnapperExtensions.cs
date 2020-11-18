using System.Linq;
using Itinero.Network;

namespace Itinero.Snapping
{
    public static class ILocationsSnapperExtensions
    {
        /// <summary>
        /// Snap to the given location.
        /// </summary>
        /// <param name="locationsSnapper">The snappable.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns></returns>
        public static Result<SnapPoint> To(this ILocationsSnapper locationsSnapper, double longitude, double latitude)
        {
            return locationsSnapper.To(new[] {(longitude, latitude)}).First();
        }
        
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