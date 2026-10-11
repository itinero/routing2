using System.Collections.Generic;

namespace Itinero.Routing.Isochrones;

/// <summary>
/// A closed ring of coordinates, first point not repeated as the last.
/// </summary>
public sealed class IsochroneRing
{
    internal IsochroneRing(List<(double longitude, double latitude)> coordinates, double signedArea)
    {
        this.Coordinates = coordinates;
        this.SignedArea = signedArea;
    }

    public IReadOnlyList<(double longitude, double latitude)> Coordinates { get; }

    /// <summary>
    /// Twice the signed area in square degrees. Positive is counter-clockwise.
    /// </summary>
    /// <remarks>
    /// Degrees, so it compares rings at one latitude and is not an area measurement.
    /// </remarks>
    internal double SignedArea { get; }
}

/// <summary>
/// One reachable area: an outer ring, and holes for anything inside it the search never reached.
/// </summary>
/// <remarks>
/// A hole is a real answer, not an artefact — a lake, or a block whose streets are all one-way
/// outward. Bands can have several areas when the network is disconnected at this cost.
/// </remarks>
public sealed class IsochronePolygon
{
    internal IsochronePolygon(IsochroneRing outer, List<IsochroneRing> holes)
    {
        this.Outer = outer;
        this.Holes = holes;
    }

    public IsochroneRing Outer { get; }

    public IReadOnlyList<IsochroneRing> Holes { get; }
}

/// <summary>
/// Everything reachable from the origin at no more than <see cref="Cost"/>.
/// </summary>
public sealed class IsochroneBand
{
    internal IsochroneBand(double cost, List<IsochronePolygon> polygons)
    {
        this.Cost = cost;
        this.Polygons = polygons;
    }

    /// <summary>The cost bound this band was cut at, in the cost function's own units.</summary>
    public double Cost { get; }

    public IReadOnlyList<IsochronePolygon> Polygons { get; }
}

/// <summary>
/// The bands, innermost first, and what the search behind them cost.
/// </summary>
public sealed class Isochrone
{
    internal Isochrone(List<IsochroneBand> bands, IsochroneDiagnostics diagnostics)
    {
        this.Bands = bands;
        this.Diagnostics = diagnostics;
    }

    public IReadOnlyList<IsochroneBand> Bands { get; }

    public IsochroneDiagnostics Diagnostics { get; }
}

/// <param name="Settled">States the search settled, including those past the outermost band.</param>
/// <param name="Reached">Vertices given a cost.</param>
/// <param name="Cells">Grid cells the cost surface covers.</param>
/// <param name="CellSizeMeters">Resolution actually used, which may be coarser than asked for.</param>
public readonly record struct IsochroneDiagnostics(
    long Settled,
    int Reached,
    long Cells,
    double CellSizeMeters);
