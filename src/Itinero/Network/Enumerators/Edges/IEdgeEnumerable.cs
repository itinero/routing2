using Itinero.Network.Tiles;

namespace Itinero.Network.Enumerators.Edges;

public interface IEdgeEnumerable
{
    internal NetworkTile? GetTileForRead(uint tileId);

    /// <summary>
    /// The db this is a network of, for what is stable across generations: the type indexes.
    /// </summary>
    internal RouterDb RouterDb { get; }

    /// <summary>
    /// What resolves this network's edge ids, which is the only thing that can: ids belong to the
    /// network that issued them, not to the db.
    /// </summary>
    internal Data.Providers.DataProviderBase Provider { get; }
}
