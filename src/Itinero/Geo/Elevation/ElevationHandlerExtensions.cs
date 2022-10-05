namespace Itinero.Geo.Elevation
{
    /// <summary>
    /// Contains extension methods to add/handle elevation.
    /// </summary>
    public static class ElevationHandlerExtensions
    {
        /// <summary>
        /// Adds elevation to a given coordinate.
        /// </summary>
        /// <param name="coordinate">The coordinate.</param>
        /// <param name="estimate">The estimate value if there is no handler configured.</param>
        /// <param name="elevationHandler">The elevation handler, if null, the default will be used.</param>
        /// <returns>The same coordinate but with an elevation component is available.</returns>
        public static (double lon, double lat, float? e) AddElevation(this (double lon, double lat) coordinate,
            float? estimate = null, IElevationHandler? elevationHandler = null)
        {
            elevationHandler ??= ElevationHandler.Default;

            var e = elevationHandler?.Elevation(coordinate.lon, coordinate.lat)
                    ?? estimate;

            return (coordinate.lon, coordinate.lat, e);
        }

        /// <summary>
        /// Adds elevation to a given coordinate.
        /// </summary>
        /// <param name="coordinate">The coordinate.</param>
        /// <param name="estimate">The estimate value if there is no handler configured.</param>
        /// <param name="elevationHandler">The elevation handler, if null, the default will be used.</param>
        /// <returns>The same coordinate but with an elevation component is available.</returns>
        public static (double lon, double lat, float? e) AddElevation(
            this (double lon, double lat, float? e) coordinate,
            float? estimate = null, IElevationHandler? elevationHandler = null)
        {
            if (coordinate.e != null)
            {
                return coordinate;
            }

            return (coordinate.lon, coordinate.lat).AddElevation(estimate, elevationHandler);
        }
    }
}
