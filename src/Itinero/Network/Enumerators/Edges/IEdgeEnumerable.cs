using Itinero.Network.Tiles;

namespace Itinero.Network.Enumerators.Edges;

public interface IEdgeEnumerable
{
    internal NetworkTile? GetTileForRead(uint tileId);

    /// <summary>
    /// The db this is a network of, which is how an edge's data is reached: the tile holds routing
    /// data only, so everything else comes from the db's provider.
    /// </summary>
    internal RouterDb RouterDb { get; }
}
