using System;
using System.Collections.Generic;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Islands;
using Itinero.Network.Tiles;

namespace Itinero.Network.Writer;

internal interface IRoutingNetworkWritable
{
    int Zoom { get; }

    RoutingNetworkIslandManager IslandManager { get; }

    RouterDb RouterDb { get; }

    bool TryGetVertex(VertexId vertexId, out double longitude, out double latitude, out float? elevation);

    internal RoutingNetworkEdgeEnumerator GetEdgeEnumerator();

    (NetworkTile tile, Func<IEnumerable<(string key, string value)>, uint> func) GetTileForWrite(uint localTileId);

    void SetTile(NetworkTile tile);

    bool HasTile(uint localTileId);

    void ClearWriter();

    /// Releases one concurrent tile-insert writer.
    void ReleaseTileWriter();

    /// <summary>
    /// The stripe index whose lock guards writes to this tile.
    /// </summary>
    /// <remarks>
    /// Callers that need several locks must order by THIS index, not by tile id. Ordering by tile
    /// id would be wrong under striping: tiles 5 and 4100 map to stripes 5 and 4, so taking them
    /// in tile order takes stripe 5 then 4, while tiles 4 and 4101 take stripe 4 then 5 — a
    /// cycle, and a deadlock. The total order has to be over the things actually locked.
    /// </remarks>
    int TileLockIndex(uint localTileId);

    /// The lock for a stripe. Monitor is re-entrant, so a caller already holding it may pass
    /// through code that takes it again.
    object GetTileLockByIndex(int index);
}
