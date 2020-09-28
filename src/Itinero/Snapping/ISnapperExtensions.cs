using Itinero.Profiles;

namespace Itinero.Snapping
{
    public static class ISnapperExtensions
    {
        /// <summary>
        /// Snaps to an edge closest to the given coordinates.
        /// </summary>
        /// <param name="snapper">The snapper.</param>
        /// <param name="location">The location.</param>
        /// <param name="profile">The profile.</param>
        /// <returns>The snap point.</returns>
        public static Result<SnapPoint> To(this ISnapper snapper, (double longitude, double latitude) location, Profile profile)
        {
            return snapper.Snap(location, new SnapPointSettings()
            {
                Profiles = new []{profile}
            });
        }
    }
}