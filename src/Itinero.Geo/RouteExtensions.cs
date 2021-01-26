using System.Linq;
using Itinero.Routes;
using NetTopologySuite.Geometries;

namespace Itinero.Geo {
    /// <summary>
    /// Contains extension methods for the route object.
    /// </summary>
    public static class RouteExtensions {
        /// <summary>
        /// Converts the given route to a line string.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <returns>The linestring.</returns>
        public static LineString ToLineString(this Route route) {
            return new LineString(route.Shape.Select(x => new Coordinate(x.longitude, x.latitude)).ToArray());
        }
    }
}