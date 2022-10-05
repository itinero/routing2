namespace Itinero.Geo.Elevation
{
    /// <summary>
    /// Abstract representation of an elevation handler.
    /// </summary>
    public interface IElevationHandler
    {
        /// <summary>
        /// Add elevation to the given coordinate.
        /// </summary>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <returns>The elevation associated with the given coordinates, if any.</returns>
        public float? Elevation(double longitude, double latitude);
    }
}
