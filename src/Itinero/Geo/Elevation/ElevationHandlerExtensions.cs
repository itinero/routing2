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
        /// <returns>The same coordinate but with an elevation component is available.</returns>
        public static (double lon, double lat, float? e) AddElevation(this (double lon, double lat) coordinate,
            float? estimate)
        {
            var e = coordinate.Elevation() ?? estimate;

            return (coordinate.lon, coordinate.lat, e);
        }
        
        /// <summary>
        /// Adds elevation to a given coordinate.
        /// </summary>
        /// <param name="coordinate">The coordinate.</param>
        /// <param name="estimate">The estimate value if there is no handler configured.</param>
        /// <returns>The same coordinate but with an elevation component is available.</returns>
        public static (double lon, double lat, float? e) AddElevation(this (double lon, double lat, float? e) coordinate,
            float? estimate)
        {
            if (coordinate.e != null) return coordinate;
            
            var e = (coordinate.lon, coordinate.lat).Elevation() ?? estimate;

            return (coordinate.lon, coordinate.lat, e);
        }
    }
}