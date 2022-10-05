using Itinero.Network.Tiles;

namespace Itinero.Network.Enumerators.Edges;

public interface IEdgeEnumerable
{
    internal NetworkTile? GetTileForRead(uint tileId);
}
