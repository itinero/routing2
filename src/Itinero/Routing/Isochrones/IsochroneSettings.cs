using System;

namespace Itinero.Routing.Isochrones;

/// <summary>
/// How finely to resolve an isochrone, and how far past it to search.
/// </summary>
public class IsochroneSettings
{
    /// <summary>
    /// Grid resolution in metres, or null to scale it to the area reached.
    /// </summary>
    /// <remarks>
    /// A fixed size is too coarse for a short walk and too fine for an hour's drive. Coarsened
    /// further if <see cref="MaxCells"/> would be exceeded.
    /// </remarks>
    public double? CellSizeMeters { get; set; }

    /// <summary>Cells across the reached area when <see cref="CellSizeMeters"/> is null.</summary>
    public int TargetCellsAcross { get; set; } = 400;

    /// <summary>Floor on the scaled cell size, in metres.</summary>
    public double MinCellSizeMeters { get; set; } = 20.0;

    /// <summary>
    /// Ceiling on grid cells, so a large bound coarsens rather than allocating without limit.
    /// </summary>
    public long MaxCells { get; set; } = 8_000_000;

    /// <summary>
    /// How far past the outermost band to keep searching, as a share of that band.
    /// </summary>
    /// <remarks>
    /// The boundary is interpolated between a point inside it and one outside, so stopping at
    /// the band leaves nothing to interpolate against and the contour pulls inward.
    /// </remarks>
    public double Margin { get; set; } = 0.15;

    /// <summary>
    /// Drop any ring enclosing less than this share of the band's largest ring.
    /// </summary>
    /// <remarks>
    /// Single cells get picked up where a road clips the corner of one. 0 keeps everything.
    /// </remarks>
    public double Denoise { get; set; } = 0.01;

    /// <summary>
    /// How far off a road still counts as reached, in metres. 0 marks only the roads.
    /// </summary>
    /// <remarks>
    /// Without this a band is a set of ribbons as wide as one grid cell, which only reads as an
    /// area where roads are closer together than that. Valhalla gets the same effect from cells
    /// of 110-550 m, where the ribbon is a cell wide; separating the two keeps the boundary as
    /// accurate as the grid while the fill is wide enough to join.
    /// </remarks>
    public double ReachRadiusMeters { get; set; } = 150.0;

    /// <summary>
    /// Fill unreached pockets smaller than this, in square metres. 0 keeps every pocket.
    /// </summary>
    /// <remarks>
    /// The surface only knows costs along roads, so without this every city block comes back as
    /// a hole and the band describes a street network rather than an area. The default fills a
    /// block of roughly 220 m a side and leaves genuinely unreachable ground — water, a closed
    /// estate — as the hole it is.
    /// </remarks>
    public double FillHolesBelowSquareMeters { get; set; } = 50_000.0;

    internal void Validate()
    {
        if (this.CellSizeMeters is <= 0)
            throw new ArgumentOutOfRangeException(nameof(this.CellSizeMeters));
        if (this.TargetCellsAcross < 2)
            throw new ArgumentOutOfRangeException(nameof(this.TargetCellsAcross));
        if (this.MinCellSizeMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(this.MinCellSizeMeters));
        if (this.MaxCells < 4) throw new ArgumentOutOfRangeException(nameof(this.MaxCells));
        if (this.Margin < 0) throw new ArgumentOutOfRangeException(nameof(this.Margin));
        if (this.Denoise is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(this.Denoise));
        if (this.FillHolesBelowSquareMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(this.FillHolesBelowSquareMeters));
        if (this.ReachRadiusMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(this.ReachRadiusMeters));
    }
}
