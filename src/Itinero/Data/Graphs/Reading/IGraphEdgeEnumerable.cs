using Itinero.Data.Graphs.Tiles;

namespace Itinero.Data.Graphs.Reading
{
    public interface IGraphEdgeEnumerable
    {
        internal GraphTile? GetTileForRead(uint tileId);
    }
}