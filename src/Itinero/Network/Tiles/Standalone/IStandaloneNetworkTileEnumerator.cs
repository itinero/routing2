using Itinero.Network.Enumerators.Edges;

namespace Itinero.Network.Tiles.Standalone;

/// <summary>
/// Abstract representation of an edge enumerator in a standalone tile.
/// </summary>
public interface IStandaloneNetworkTileEnumerator : IEdgeEnumerator
{
    /// <summary>
    /// Gets the tile id.
    /// </summary>
    uint TileId { get; }

    /// <summary>
    /// True if the tile is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Move to the vertex.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <returns>True if the move succeeds and the vertex exists.</returns>
    bool MoveTo(VertexId vertex);

    /// <summary>
    /// Move to the given edge.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="forward">The forward flag.</param>
    /// <returns>True if the move succeeds and the edge exists.</returns>
    bool MoveTo(EdgeId edge, bool forward);
}