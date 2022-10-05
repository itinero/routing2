using System;
using System.Collections.Generic;
using Itinero.Geo.Elevation;

namespace Itinero.Geo;

/// <summary>
/// Contains extension methods to work with coordinates, lines, bounding boxes and basic spatial operations.
/// </summary>
public static class GeoExtensions
{
    /// <summary>
    /// Returns an estimate of the distance between the two given coordinates.
    /// </summary>
    /// <param name="coordinate1">The first coordinate.</param>
    /// <param name="coordinate2">The second coordinate.</param>
    /// <remarks>Accuracy decreases with distance.</remarks>
    public static double DistanceEstimateInMeter(this (double longitude, double latitude, float? e) coordinate1,
        (double longitude, double latitude, float? e) coordinate2)
    {
        var lat1Rad = coordinate1.latitude / 180d * Math.PI;
        var lon1Rad = coordinate1.longitude / 180d * Math.PI;
        var lat2Rad = coordinate2.latitude / 180d * Math.PI;
        var lon2Rad = coordinate2.longitude / 180d * Math.PI;

        var x = (lon2Rad - lon1Rad) * Math.Cos((lat1Rad + lat2Rad) / 2.0);
        var y = lat2Rad - lat1Rad;

        var m = Math.Sqrt((x * x) + (y * y)) * Constants.RadiusOfEarth;

        return m;
    }

    internal static double DistanceEstimateInMeterShape(
        this (double longitude, double latitude, float? e) coordinate1,
        (double longitude, double latitude, float? e) coordinate2,
        IEnumerable<(double longitude, double latitude, float? e)>? shape = null)
    {
        if (shape == null)
        {
            return coordinate1.DistanceEstimateInMeter(coordinate2);
        }

        var distance = 0.0;

        using var shapeEnumerator = shape.GetEnumerator();
        var previous = coordinate1;

        while (shapeEnumerator.MoveNext())
        {
            var current = shapeEnumerator.Current;
            distance += previous.DistanceEstimateInMeter(current);
            previous = current;
        }

        distance += previous.DistanceEstimateInMeter(coordinate2);

        return distance;
    }

    /// <summary>
    /// Returns an estimate of the length of the given linestring.
    /// </summary>
    /// <param name="lineString">The linestring.</param>
    /// <remarks>Accuracy decreases with distance.</remarks>
    public static double DistanceEstimateInMeter(
        this IEnumerable<(double longitude, double latitude, float? e)> lineString)
    {
        var distance = 0.0;

        using var shapeEnumerator = lineString.GetEnumerator();
        shapeEnumerator.MoveNext();
        var previous = shapeEnumerator.Current;

        while (shapeEnumerator.MoveNext())
        {
            var current = shapeEnumerator.Current;
            distance += previous.DistanceEstimateInMeter(current);
            previous = current;
        }

        return distance;
    }

    /// <summary>
    /// Returns a coordinate offset with a given distance.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <param name="meter">The distance.</param>
    /// <returns>An offset coordinate.</returns>
    public static (double longitude, double latitude, float? e) OffsetWithDistanceX(
        this (double longitude, double latitude, float? e) coordinate, double meter)
    {
        var offset = 0.001;
        var offsetLon = (coordinate.longitude + offset, coordinate.latitude).AddElevation(coordinate.e);
        var lonDistance = offsetLon.DistanceEstimateInMeter(coordinate);

        return (coordinate.longitude + (meter / lonDistance * offset),
            coordinate.latitude).AddElevation(coordinate.e);
    }

    /// <summary>
    /// Returns a coordinate offset with a given distance.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <param name="meter">The distance.</param>
    /// <returns>An offset coordinate.</returns>
    public static (double longitude, double latitude, float? e) OffsetWithDistanceY(
        this (double longitude, double latitude, float? e) coordinate,
        double meter)
    {
        var offset = 0.001;
        var offsetLat = (coordinate.longitude, coordinate.latitude + offset).AddElevation(coordinate.e);
        var latDistance = offsetLat.DistanceEstimateInMeter(coordinate);

        return (coordinate.longitude,
            coordinate.latitude + (meter / latDistance * offset)).AddElevation(coordinate.e);
    }

    /// <summary>
    /// Calculates an offset position along the line segment.
    /// </summary>
    /// <param name="line">The line segment.</param>
    /// <param name="offset">The offset [0,1].</param>
    /// <returns>The offset coordinate.</returns>
    public static (double longitude, double latitude, float? e) PositionAlongLine(
        this ((double longitude, double latitude, float? e) coordinate1,
            (double longitude, double latitude, float? e) coordinate2) line, double offset)
    {
        var coordinate1 = line.coordinate1;
        var coordinate2 = line.coordinate2;

        var latitude = coordinate1.latitude + ((coordinate2.latitude - coordinate1.latitude) * offset);
        var longitude = coordinate1.longitude + ((coordinate2.longitude - coordinate1.longitude) * offset);
        float? e = null;
        if (coordinate1.e.HasValue &&
            coordinate2.e.HasValue)
        {
            e = (float)(coordinate1.e.Value - ((coordinate2.e.Value - coordinate1.e.Value) * offset));
        }

        return (longitude, latitude).AddElevation(e);
    }

    private const double E = 0.0000000001;

    /// <summary>
    /// Projects for coordinate on this line.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="coordinate">The coordinate.</param>
    /// <returns>The project coordinate.</returns>
    public static (double longitude, double latitude, float? e)? ProjectOn(
        this ((double longitude, double latitude, float? e) coordinate1,
            (double longitude, double latitude, float? e) coordinate2) line,
        (double longitude, double latitude, float? e) coordinate)
    {
        var coordinate1 = line.coordinate1;
        var coordinate2 = line.coordinate2;

        // TODO: do we need to calculate the expensive length in meter, this can be done more easily.
        var lengthInMeters = line.coordinate1.DistanceEstimateInMeter(line.coordinate2);
        if (lengthInMeters < E)
        {
            return null;
        }

        // get direction vector.
        var diffLat = coordinate2.latitude - coordinate1.latitude;
        var diffLon = coordinate2.longitude - coordinate1.longitude;

        // increase this line in length if needed.
        var longerLine = line;
        if (lengthInMeters < 50)
        {
            longerLine = (coordinate1, (diffLon + coordinate.longitude, diffLat + coordinate.latitude, null));
        }

        // rotate 90°, offset y with x, and x with y.
        var xLength = longerLine.coordinate1.DistanceEstimateInMeter((longerLine.coordinate2.longitude,
            longerLine.coordinate1.latitude, null));
        if (longerLine.coordinate1.longitude > longerLine.coordinate2.longitude)
        {
            xLength = -xLength;
        }

        var yLength = longerLine.coordinate1.DistanceEstimateInMeter((longerLine.coordinate1.longitude,
            longerLine.coordinate2.latitude, null));
        if (longerLine.coordinate1.latitude > longerLine.coordinate2.latitude)
        {
            yLength = -yLength;
        }

        var second = coordinate.OffsetWithDistanceY(xLength)
            .OffsetWithDistanceX(-yLength);

        // create a second line.
        var other = (coordinate, second);

        // calculate intersection.
        var projected = longerLine.Intersect(other, false);

        // check if coordinate is on this line.
        if (!projected.HasValue)
        {
            return null;
        }

        // check if the coordinate is on this line.
        var dist = (line.A() * line.A()) + (line.B() * line.B());
        var line1 = (projected.Value, coordinate1);
        var distTo1 = (line1.A() * line1.A()) + (line1.B() * line1.B());
        if (distTo1 > dist)
        {
            return null;
        }

        var line2 = (projected.Value, coordinate2);
        var distTo2 = (line2.A() * line2.A()) + (line2.B() * line2.B());
        if (distTo2 > dist)
        {
            return null;
        }

        return projected;
    }

    /// <summary>
    /// Returns the center of the box.
    /// </summary>
    /// <param name="box">The box.</param>
    /// <returns>The center.</returns>
    public static (double longitude, double latitude, float? e) Center(
        this ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) box)
    {
        float? e = null;
        if (box.topLeft.e.HasValue &&
            box.bottomRight.e.HasValue)
        {
            e = box.topLeft.e.Value + box.bottomRight.e.Value;
        }

        return ((box.topLeft.longitude + box.bottomRight.longitude) / 2,
            (box.topLeft.latitude + box.bottomRight.latitude) / 2).AddElevation(e);
    }

    /// <summary>
    /// Expands the given box with the other box to encompass both.
    /// </summary>
    /// <param name="box">The original box.</param>
    /// <param name="other">The other box.</param>
    /// <returns>The expand box or the original box if the other was already contained.</returns>
    public static ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float?
        e) bottomRight)
        Expand(
            this ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float?
                e) bottomRight) box,
            ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
                bottomRight) other)
    {
        if (!box.Overlaps(other.topLeft))
        {
            var center = box.Center();

            // handle left.
            var left = box.topLeft.longitude;
            if (!box.Overlaps((other.topLeft.longitude, center.latitude, null)))
            {
                left = other.topLeft.longitude;
            }

            // handle top.
            var top = box.topLeft.longitude;
            if (!box.Overlaps((center.longitude, other.topLeft.latitude, null)))
            {
                top = other.topLeft.latitude;
            }

            box = ((left, top, null), box.bottomRight);
        }

        if (!box.Overlaps(other.bottomRight))
        {
            var center = box.Center();

            // handle right.
            var right = box.bottomRight.longitude;
            if (!box.Overlaps((other.bottomRight.longitude, center.latitude, null)))
            {
                right = other.bottomRight.longitude;
            }

            // handle bottom.
            var bottom = box.bottomRight.latitude;
            if (!box.Overlaps((center.longitude, other.bottomRight.latitude, null)))
            {
                bottom = other.bottomRight.latitude;
            }

            box = (box.topLeft, (right, bottom, null));
        }

        return box;
    }

    /// <summary>
    /// Calculates the intersection point of the given line with this line. 
    /// 
    /// Returns null if the lines have the same direction or don't intersect.
    /// 
    /// Assumes the given line is not a segment and this line is a segment.
    /// </summary>
    public static (double longitude, double latitude, float? e)? Intersect(
        this ((double longitude, double latitude, float? e) coordinate1,
            (double longitude, double latitude, float? e) coordinate2) thisLine,
        ((double longitude, double latitude, float? e) coordinate1,
            (double longitude, double latitude, float? e) coordinate2) line, bool checkSegment = true)
    {
        var det = (double)((line.A() * thisLine.B()) - (thisLine.A() * line.B()));
        if (Math.Abs(det) <= E)
        { // lines are parallel; no intersections.
            return null;
        }
        else
        { // lines are not the same and not parallel so they will intersect.
            var x = ((thisLine.B() * line.C()) - (line.B() * thisLine.C())) / det;
            var y = ((line.A() * thisLine.C()) - (thisLine.A() * line.C())) / det;

            (double latitude, double longitude, float? e) coordinate = (x, y, (float?)null);

            // check if the coordinate is on this line.
            if (checkSegment)
            {
                var dist = (thisLine.A() * thisLine.A()) + (thisLine.B() * thisLine.B());
                var line1 = (coordinate, thisLine.coordinate1);
                var distTo1 = (line1.A() * line1.A()) + (line1.B() * line1.B());
                if (distTo1 > dist)
                {
                    return null;
                }

                var line2 = (coordinate, thisLine.coordinate2);
                var distTo2 = (line2.A() * line2.A()) + (line2.B() * line2.B());
                if (distTo2 > dist)
                {
                    return null;
                }
            }

            if (thisLine.coordinate1.e == null || thisLine.coordinate2.e == null)
            {
                return coordinate;
            }

            float? e = null;
            if (Math.Abs(thisLine.coordinate1.e.Value - thisLine.coordinate2.e.Value) < E)
            {
                // don't calculate anything, elevation is identical.
                e = thisLine.coordinate1.e;
            }
            else if (Math.Abs(thisLine.A()) < E && Math.Abs(thisLine.B()) < E)
            {
                // tiny segment, not stable to calculate offset
                e = thisLine.coordinate1.e;
            }
            else
            { // calculate offset and calculate an estimate of the elevation.
                if (Math.Abs(thisLine.A()) > Math.Abs(thisLine.B()))
                {
                    var diffLat = Math.Abs(thisLine.A());
                    var diffLatIntersection = Math.Abs(coordinate.latitude - thisLine.coordinate1.latitude);

                    e = (float)(((thisLine.coordinate2.e - thisLine.coordinate1.e) *
                                 (diffLatIntersection / diffLat)) +
                                 thisLine.coordinate1.e);
                }
                else
                {
                    var diffLon = Math.Abs(thisLine.B());
                    var diffLonIntersection = Math.Abs(coordinate.longitude - thisLine.coordinate1.longitude);

                    e = (float)(((thisLine.coordinate2.e - thisLine.coordinate1.e) *
                                 (diffLonIntersection / diffLon)) +
                                 thisLine.coordinate1.e);
                }
            }

            return coordinate.AddElevation(e);
        }
    }

    private static double A(this ((double longitude, double latitude, float? e) coordinate1,
        (double longitude, double latitude, float? e) coordinate2) line)
    {
        return line.coordinate2.latitude - line.coordinate1.latitude;
    }

    private static double B(this ((double longitude, double latitude, float? e) coordinate1,
        (double longitude, double latitude, float? e) coordinate2) line)
    {
        return line.coordinate1.longitude - line.coordinate2.longitude;
    }

    private static double C(this ((double longitude, double latitude, float? e) coordinate1,
        (double longitude, double latitude, float? e) coordinate2) line)
    {
        return (line.A() * line.coordinate1.longitude) +
               (line.B() * line.coordinate1.latitude);
    }

    /// <summary>
    /// Creates a box around this coordinate with width/height approximately the given size in meter.
    /// </summary>
    /// <param name="coordinate">The coordinate.</param>
    /// <param name="sizeInMeters">The size in meter.</param>
    /// <returns>The size in meter.</returns>
    public static ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float?
        e) bottomRight)
        BoxAround(this (double longitude, double latitude, float? e) coordinate, double sizeInMeters)
    {
        var offsetLat = (coordinate.longitude, coordinate.latitude + 0.1, (float?)null);
        var offsetLon = (coordinate.longitude + 0.1, coordinate.latitude, (float?)null);
        var latDistance = offsetLat.DistanceEstimateInMeter(coordinate);
        var lonDistance = offsetLon.DistanceEstimateInMeter(coordinate);

        return ((coordinate.longitude - (sizeInMeters / lonDistance * 0.1),
                coordinate.latitude + (sizeInMeters / latDistance * 0.1), null),
            (coordinate.longitude + (sizeInMeters / lonDistance * 0.1),
                coordinate.latitude - (sizeInMeters / latDistance * 0.1), null));
    }

    /// <summary>
    /// Returns true if the box overlaps the given coordinate.
    /// </summary>
    /// <param name="box">The box.</param>
    /// <param name="coordinate">The coordinate.</param>
    /// <returns>True of the coordinate is inside the bounding box.</returns>
    public static bool Overlaps(
        this ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) box,
        (double longitude, double latitude, float? e) coordinate)
    {
        return box.bottomRight.latitude < coordinate.latitude && coordinate.latitude <= box.topLeft.latitude &&
               box.topLeft.longitude < coordinate.longitude && coordinate.longitude <= box.bottomRight.longitude;
    }

    /// <summary>
    /// Given two WGS84 coordinates, if walking from c1 to c2, it gives the angle that one would be following.
    /// 
    /// 0° is north, 90° is east, -90° is west, both 180 and -180 are south
    /// </summary>
    /// <param name="c1">The first coordinate.</param>
    /// <param name="c2">The second coordinate.</param>
    /// <returns>The angle with the meridian in Northern direction.</returns>
    public static double AngleWithMeridian(this (double longitude, double latitude, float? e) c1,
        (double longitude, double latitude, float? e) c2)
    {
        var dy = c2.latitude - c1.latitude;
        var dx = Math.Cos(Math.PI / 180 * c1.latitude) * (c2.longitude - c1.longitude);
        // phi is the angle we search, but with 0 pointing eastwards and in radians
        var phi = Math.Atan2(dy, dx);
        var angle =
            (phi - (Math.PI / 2)) // Rotate 90° to have the north up
            * 180 / Math.PI; // Convert to degrees
        angle = -angle;
        // A bit of normalization below:
        if (angle < -180)
        {
            angle += 360;
        }

        if (angle > 180)
        {
            angle -= 360;
        }

        return angle;
    }
}
