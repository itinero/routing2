namespace Itinero.Geo.Elevation
{
    /// <summary>
    /// The default elevation handler.
    /// </summary>
    public class ElevationHandler : IElevationHandler
    {
        private readonly GetElevationDelegate _getElevation;

        /// <summary>
        /// Creates a new elevation handler.
        /// </summary>
        /// <param name="getElevation">The function to get elevation from.</param>
        public ElevationHandler(GetElevationDelegate getElevation)
        {
            _getElevation = getElevation;
        }

        /// <summary>
        /// A delegate to get elevation.
        /// </summary>
        public delegate float? GetElevationDelegate(double longitude, double latitude);

        /// <summary>
        /// Add elevation to the given coordinate.
        /// </summary>
        public float? Elevation(double longitude, double latitude)
        {
            return _getElevation?.Invoke(longitude, latitude);
        }

        /// <summary>
        /// Gets or sets the default elevation handler.
        /// </summary>
        public static ElevationHandler? Default { get; set; }
    }
}