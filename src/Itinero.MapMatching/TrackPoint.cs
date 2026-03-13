using System;

namespace Itinero.MapMatching;

/// <summary>
/// Represents a track point.
/// </summary>
public readonly struct TrackPoint
{
    /// <summary>
    /// Creates a new track point.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    public TrackPoint(double longitude, double latitude)
    {
        this.Location = (longitude, latitude);
        this.Timestamp = null;
    }

    /// <summary>
    /// Creates a new track point with a timestamp.
    /// </summary>
    /// <param name="longitude">The longitude.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="timestamp">The timestamp.</param>
    public TrackPoint(double longitude, double latitude, DateTimeOffset timestamp)
    {
        this.Location = (longitude, latitude);
        this.Timestamp = timestamp;
    }

    /// <summary>
    /// Gets the location.
    /// </summary>
    public (double longitude, double latitude) Location { get; }

    /// <summary>
    /// Gets the timestamp, if any.
    /// </summary>
    public DateTimeOffset? Timestamp { get; }
}
