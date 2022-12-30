using NetTopologySuite.Geometries;

namespace Itinero.Geo;

/// <summary>
/// Extension methods related to coordinates.
/// </summary>
public static class CoordinateExtensions
{
    /// <summary>
    /// Converts the coordinate to an Itinero coordinate tuple.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <returns>A coordinate tuple.</returns>
    public static (double longitude, double latitude, float? e) ToCoordinateTuple(this Coordinate coordinate)
    {
        return (coordinate.X, coordinate.Y, null);
    }

    /// <summary>
    /// Returns an estimate of the distance between the two given coordinates.
    /// </summary>
    /// <param name="coordinate">The first coordinate.</param>
    /// <param name="other">The second coordinate.</param>
    /// <remarks>Accuracy decreases with distance.</remarks>
    public static double DistanceEstimateInMeter(this Coordinate coordinate, Coordinate other)
    {
        return coordinate.ToCoordinateTuple().DistanceEstimateInMeter(other.ToCoordinateTuple());
    }
}
