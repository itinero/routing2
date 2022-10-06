namespace Itinero.Network.Tiles.Standalone;

/// <summary>
/// An id that can represent edge that are part of tiles or boundary edges.
/// </summary>
public readonly struct BoundaryOrLocalEdgeId
{
    /// <summary>
    /// Creates an id using an edge id.
    /// </summary>
    /// <param name="edgeId"></param>
    public BoundaryOrLocalEdgeId(EdgeId edgeId)
    {
        this.LocalId = edgeId;
        this.BoundaryId = null;
    }

    /// <summary>
    /// Creates an id using a boundary edge id.
    /// </summary>
    /// <param name="boundaryEdgeId"></param>
    public BoundaryOrLocalEdgeId(BoundaryEdgeId boundaryEdgeId)
    {
        this.BoundaryId = boundaryEdgeId;
        this.LocalId = null;
    }

    /// <summary>
    /// The local id, an edge that is fully inside the tile.
    /// </summary>
    public EdgeId? LocalId { get; }

    /// <summary>
    /// A boundary edge, an edge that crosses tile boundaries.
    /// </summary>
    public BoundaryEdgeId? BoundaryId { get; }
}
