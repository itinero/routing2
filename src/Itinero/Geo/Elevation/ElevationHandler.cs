namespace Itinero.Geo.Elevation
{
    /// <summary>
    /// An elevation handler.
    /// </summary>
    public static class ElevationHandler
    {
        /// <summary>
        /// Gets or sets the delegate to get elevation.
        /// </summary>
        public static GetElevationDelegate? GetElevation { get; set; }

        /// <summary>
        /// A delegate to get elevation.
        /// </summary>
        public delegate float? GetElevationDelegate(double longitude, double latitude);

        /// <summary>
        /// Add elevation to the given coordinate.
        /// </summary>
        public static float? Elevation(this (double longitude, double latitude) coordinate)
        {
            return GetElevation?.Invoke(coordinate.longitude, coordinate.latitude);
        }
    }
}