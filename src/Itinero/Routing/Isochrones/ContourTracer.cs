using System;
using System.Collections.Generic;
using System.Threading;

namespace Itinero.Routing.Isochrones;

/// <summary>
/// Marching squares over a cost grid: turns "cost at each grid corner" into closed rings at a
/// given cost.
/// </summary>
/// <remarks>
/// Segments are emitted with the reachable side on the left, so an outer boundary comes out
/// counter-clockwise and a hole clockwise — which is both how they are told apart here and the
/// winding GeoJSON asks for.
/// </remarks>
internal static class ContourTracer
{
    // Which cell side a crossing sits on. Used to give every crossing an exact integer identity,
    // so segments are joined by lattice position rather than by comparing interpolated doubles.
    private const int Bottom = 0;
    private const int Right = 1;
    private const int Top = 2;
    private const int Left = 3;

    /// <summary>
    /// Traces the band at <paramref name="band"/> out of a corner-cost grid.
    /// </summary>
    /// <param name="cost">Cost per corner, row-major, <see cref="double.MaxValue"/> if unreached.</param>
    /// <param name="width">Corners per row.</param>
    /// <param name="height">Rows of corners.</param>
    public static List<IsochronePolygon> Trace(double[] cost, int width, int height, double band,
        double minLon, double minLat, double lonPerCell, double latPerCell, double denoise,
        double squareMetersPerSquareDegree = 0, double fillHolesBelow = 0,
        CancellationToken cancellationToken = default)
    {
        var segments = new List<(long from, long to)>();
        var points = new Dictionary<long, (double longitude, double latitude)>();

        for (var y = 0; y < height - 1; y++)
        {
            // Per row: a band over a wide area is millions of cells, once per band.
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < width - 1; x++)
            {
                var c0 = cost[y * width + x];
                var c1 = cost[y * width + x + 1];
                var c2 = cost[(y + 1) * width + x + 1];
                var c3 = cost[(y + 1) * width + x];

                var mask = 0;
                if (c0 <= band) mask |= 1;
                if (c1 <= band) mask |= 2;
                if (c2 <= band) mask |= 4;
                if (c3 <= band) mask |= 8;
                if (mask == 0 || mask == 15) continue;

                switch (mask)
                {
                    case 1: Add(Bottom, Left); break;
                    case 2: Add(Right, Bottom); break;
                    case 3: Add(Right, Left); break;
                    case 4: Add(Top, Right); break;
                    case 6: Add(Top, Bottom); break;
                    case 7: Add(Top, Left); break;
                    case 8: Add(Left, Top); break;
                    case 9: Add(Bottom, Top); break;
                    case 11: Add(Right, Top); break;
                    case 12: Add(Left, Right); break;
                    case 13: Add(Bottom, Right); break;
                    case 14: Add(Left, Bottom); break;

                    // Saddles. The two diagonal corners may be one region pinched through the
                    // cell or two separate ones, and the grid cannot tell; the cell's mean
                    // decides, which is the usual convention and keeps rings closed either way.
                    case 5:
                        if (Centre() <= band) { Add(Bottom, Right); Add(Top, Left); }
                        else { Add(Bottom, Left); Add(Top, Right); }
                        break;
                    case 10:
                        if (Centre() <= band) { Add(Left, Bottom); Add(Right, Top); }
                        else { Add(Right, Bottom); Add(Left, Top); }
                        break;
                }

                continue;

                double Centre()
                {
                    // An unreached corner has no finite cost to average, so the mean is taken
                    // over the reached ones only; all four unreached cannot reach a saddle.
                    var sum = 0d;
                    var n = 0;
                    foreach (var c in stackalloc[] { c0, c1, c2, c3 })
                    {
                        if (c >= double.MaxValue) continue;
                        sum += c;
                        n++;
                    }

                    return n == 0 ? double.MaxValue : sum / n;
                }

                void Add(int fromSide, int toSide)
                {
                    var from = Crossing(fromSide);
                    var to = Crossing(toSide);
                    if (from != to) segments.Add((from, to));
                }

                long Crossing(int side)
                {
                    // Horizontal sides are keyed by the corner left of them, vertical sides by
                    // the corner below, so the two cells sharing a side agree on its identity.
                    long key;
                    double a, b;
                    int ax, ay, bx, by;
                    switch (side)
                    {
                        case Bottom: key = Key(0, x, y); a = c0; b = c1; ax = x; ay = y; bx = x + 1; by = y; break;
                        case Right: key = Key(1, x + 1, y); a = c1; b = c2; ax = x + 1; ay = y; bx = x + 1; by = y + 1; break;
                        case Top: key = Key(0, x, y + 1); a = c3; b = c2; ax = x; ay = y + 1; bx = x + 1; by = y + 1; break;
                        default: key = Key(1, x, y); a = c0; b = c3; ax = x; ay = y; bx = x; by = y + 1; break;
                    }

                    if (!points.ContainsKey(key))
                    {
                        var t = Fraction(a, b, band);
                        points[key] = (
                            minLon + (ax + (bx - ax) * t) * lonPerCell,
                            minLat + (ay + (by - ay) * t) * latPerCell);
                    }

                    return key;
                }
            }
        }

        return Assemble(segments, points, denoise, squareMetersPerSquareDegree, fillHolesBelow);

        long Key(int kind, int x, int y) => ((long)kind << 60) | ((long)x << 30) | (uint)y;
    }

    /// <summary>
    /// Where between two corners the band falls. Pinned to the reached corner when the other was
    /// never reached: claiming half a cell of ground the search never saw would be an invention.
    /// </summary>
    private static double Fraction(double a, double b, double band)
    {
        if (a >= double.MaxValue) return b >= double.MaxValue ? 0.5 : 1.0;
        if (b >= double.MaxValue) return 0.0;

        var span = b - a;
        if (Math.Abs(span) < double.Epsilon) return 0.5;

        var t = (band - a) / span;
        return t < 0 ? 0 : t > 1 ? 1 : t;
    }

    /// <summary>
    /// Links segments end to end into rings, then nests holes under the ring containing them.
    /// </summary>
    private static List<IsochronePolygon> Assemble(List<(long from, long to)> segments,
        Dictionary<long, (double longitude, double latitude)> points, double denoise,
        double squareMetersPerSquareDegree, double fillHolesBelow)
    {
        var next = new Dictionary<long, long>(segments.Count);
        foreach (var (from, to) in segments) next[from] = to;

        var rings = new List<IsochroneRing>();
        var used = new HashSet<long>();
        foreach (var (start, _) in segments)
        {
            if (!used.Add(start)) continue;

            var coordinates = new List<(double longitude, double latitude)> { points[start] };
            var at = start;
            while (next.TryGetValue(at, out var to) && to != start)
            {
                if (!used.Add(to)) break;
                coordinates.Add(points[to]);
                at = to;
            }

            // Two points cannot enclose anything; a non-closing chain means the grid edge was
            // reached, which only happens if the bbox was too tight.
            if (coordinates.Count < 3) continue;
            rings.Add(new IsochroneRing(coordinates, SignedArea(coordinates)));
        }

        if (rings.Count == 0) return [];

        var largest = 0d;
        foreach (var ring in rings) largest = Math.Max(largest, Math.Abs(ring.SignedArea));

        var keep = new List<IsochroneRing>();
        foreach (var ring in rings)
        {
            if (Math.Abs(ring.SignedArea) >= largest * denoise) keep.Add(ring);
        }

        var polygons = new List<IsochronePolygon>();
        var outers = new List<IsochroneRing>();
        var holes = new List<IsochroneRing>();
        foreach (var ring in keep)
        {
            if (ring.SignedArea > 0) outers.Add(ring); else holes.Add(ring);
        }

        var byOuter = new Dictionary<IsochroneRing, List<IsochroneRing>>();
        foreach (var outer in outers) byOuter[outer] = [];

        foreach (var hole in holes)
        {
            // Half the shoelace sum is the area; in square degrees until scaled.
            if (fillHolesBelow > 0 && squareMetersPerSquareDegree > 0 &&
                Math.Abs(hole.SignedArea) / 2.0 * squareMetersPerSquareDegree < fillHolesBelow)
            {
                continue;
            }

            // Smallest container, so a hole inside an island inside a hole lands on the right
            // ring rather than the outermost one.
            IsochroneRing? owner = null;
            foreach (var outer in outers)
            {
                if (!Contains(outer, hole.Coordinates[0])) continue;
                if (owner == null || Math.Abs(outer.SignedArea) < Math.Abs(owner.SignedArea))
                    owner = outer;
            }

            if (owner != null) byOuter[owner].Add(hole);
        }

        foreach (var outer in outers) polygons.Add(new IsochronePolygon(outer, byOuter[outer]));

        return polygons;
    }

    private static double SignedArea(List<(double longitude, double latitude)> ring)
    {
        var sum = 0d;
        for (var i = 0; i < ring.Count; i++)
        {
            var (x1, y1) = ring[i];
            var (x2, y2) = ring[(i + 1) % ring.Count];
            sum += x1 * y2 - x2 * y1;
        }

        return sum;
    }

    private static bool Contains(IsochroneRing ring, (double longitude, double latitude) point)
    {
        var inside = false;
        var coordinates = ring.Coordinates;
        for (int i = 0, j = coordinates.Count - 1; i < coordinates.Count; j = i++)
        {
            var (xi, yi) = coordinates[i];
            var (xj, yj) = coordinates[j];
            if (yi > point.latitude != yj > point.latitude &&
                point.longitude < (xj - xi) * (point.latitude - yi) / (yj - yi) + xi)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
