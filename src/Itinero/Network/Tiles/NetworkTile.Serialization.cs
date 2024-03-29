﻿using System.IO;
using Itinero.IO;

namespace Itinero.Network.Tiles;

internal partial class NetworkTile
{
    public void WriteTo(Stream stream)
    {
        const int version = 1;
        stream.WriteVarInt32(version);

        // write tile id/zoom.
        stream.WriteVarInt32(_zoom);
        stream.WriteVarUInt32(_tileId);
        stream.WriteGuid(_edgeTypeMapId);

        // write vertices and edges.
        this.WriteEdgesAndVerticesTo(stream);

        // write attributes.
        this.WriteAttributesTo(stream);

        // write shapes.
        this.WriteGeoTo(stream);

        // write turn costs.
        this.WriteTurnCostsTo(stream);
    }

    public static NetworkTile ReadFrom(Stream stream)
    {
        var version = stream.ReadVarInt32();
        if (version != 1)
        {
            throw new InvalidDataException("Cannot deserialize tiles: Invalid version #.");
        }

        // read tile id.
        var zoom = stream.ReadVarInt32();
        var tileId = stream.ReadVarUInt32();
        var edgeTypeMapId = stream.ReadGuid();

        // create the tile.
        var graphTile = new NetworkTile(zoom, tileId, edgeTypeMapId);

        // read vertices and edges.
        graphTile.ReadEdgesAndVerticesFrom(stream);

        // read attributes.
        graphTile.ReadAttributesFrom(stream);

        // read shapes.
        graphTile.ReadGeoFrom(stream);

        // read turn costs.
        graphTile.ReadTurnCostsFrom(stream);

        return graphTile;
    }
}
