using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.MapMatching;

/// <summary>
/// Represents a track.
/// </summary>
public class Track : IEnumerable<TrackPoint>
{
    private readonly List<TrackPoint> _points;

    /// <summary>
    /// Creates a new track.
    /// </summary>
    /// <param name="points">The points.</param>
    public Track(IEnumerable<TrackPoint> points)
    {
        _points = points.ToList();
    }

    /// <summary>
    /// Gets the number of track points.
    /// </summary>
    public int Count => _points.Count;

    /// <summary>
    /// Gets the track point at the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    public TrackPoint this[int index] => _points[index];

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<TrackPoint> GetEnumerator()
    {
        return _points.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
