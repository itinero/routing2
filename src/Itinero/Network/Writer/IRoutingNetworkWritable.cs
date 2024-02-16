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
}
