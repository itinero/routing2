using Itinero.Routes;

namespace Itinero.Geo.Elevation
{
    /// <summary>
    /// Extensions for the route object related to elevation.
    /// </summary>
    public static class RouteExtensions
    {
        /// <summary>
        /// Adds elevation to the route.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="elevationHandler">The elevation handler, if null, the default will be used.</param>
        /// <remarks>
        /// This only adds elevation if an elevation handler was registered. It doesn't overwrite any elevation data already there.
        /// </remarks>
        public static void AddElevation(this Route route, IElevationHandler? elevationHandler = null)
        {
            for (var s = 0; s < route.Shape.Count; s++)
            {
                route.Shape[s] = route.Shape[s].AddElevation(elevationHandler: elevationHandler);
            }

            // TODO: add branches when implemented.
        }
    }
}
