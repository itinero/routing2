using System;
using System.IO;
using System.Linq;
using Itinero.IO;
using Itinero.Network.DataStructures;
using Itinero.Network.Mutation;
using Itinero.Network.Search.Islands;
using Itinero.Network.Tiles;

namespace Itinero.Network.Serialization;

internal static class RoutingNetworkSerializer
{
    public static void WriteTo(this RoutingNetworkMutator routingNetworkMutator, Stream stream)
    {
        // write version #.
        stream.WriteVarInt32(2);

        // write zoom.
        stream.WriteVarInt32(routingNetworkMutator.Zoom);

        // write max island size.
        stream.WriteVarInt32(routingNetworkMutator.IslandManager.MaxIslandSize);

        // write tiles.
        stream.WriteVarUInt32((uint)routingNetworkMutator.GetTiles().Count());
        foreach (var tileId in routingNetworkMutator.GetTiles())
        {
            var tile = routingNetworkMutator.GetTile(tileId);
            tile?.WriteTo(stream);
        }
    }

    public static RoutingNetwork ReadFrom(this Stream stream, RouterDb routerDb)
    {
        // check version #.
        var version = stream.ReadVarInt32();
        if (version != 2)
        {
            throw new InvalidDataException("Unknown version #.");
        }

        // read zoom.
        var zoom = stream.ReadVarInt32();

        // read max island size.
        var maxIslandSize = stream.ReadVarInt32();

        var tileCount = stream.ReadVarUInt32();
        var tiles = new SparseArray<NetworkTile?>(tileCount);
        for (var t = 0; t < tileCount; t++)
        {
            var tile = NetworkTile.ReadFrom(stream);
            tiles.EnsureMinimumSize(tile.TileId);
            tiles[tile.TileId] = tile;
        }

        return new RoutingNetwork(routerDb, tiles, zoom, new RoutingNetworkIslandManager(maxIslandSize));
    }
}
