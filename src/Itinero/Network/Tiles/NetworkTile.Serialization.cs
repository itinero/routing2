using System;
using System.IO;
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

    internal static NetworkTile ReadFromBuffer(byte[] data, ref int offset)
    {
        var version = BitCoderBuffer.GetVarInt32(data, ref offset);
        if (version != 1)
        {
            throw new InvalidDataException("Cannot deserialize tiles: Invalid version #.");
        }

        var zoom = BitCoderBuffer.GetVarInt32(data, ref offset);
        var tileId = BitCoderBuffer.GetVarUInt32(data, ref offset);
        var edgeTypeMapId = BitCoderBuffer.GetGuid(data, ref offset);

        var graphTile = new NetworkTile(zoom, tileId, edgeTypeMapId);

        graphTile.ReadEdgesAndVerticesFrom(data, ref offset);
        graphTile.ReadAttributesFrom(data, ref offset);
        graphTile.ReadGeoFrom(data, ref offset);
        graphTile.ReadTurnCostsFrom(data, ref offset);

        return graphTile;
    }

    public static NetworkTile ReadFrom(byte[] data, int offset)
    {
        return ReadFromBuffer(data, ref offset);
    }

    public byte[] ToBytes()
    {
        var maxSize = this.GetSerializedSizeUpperBound();
        var buffer = new byte[maxSize];
        var offset = 0;
        this.WriteToBuffer(buffer, ref offset);
        if (offset == maxSize) return buffer;
        var result = new byte[offset];
        Buffer.BlockCopy(buffer, 0, result, 0, offset);
        return result;
    }

    internal void WriteToBuffer(byte[] data, ref int offset)
    {
        const int version = 1;
        BitCoderBuffer.SetVarInt32(data, ref offset, version);
        BitCoderBuffer.SetVarInt32(data, ref offset, _zoom);
        BitCoderBuffer.SetVarUInt32(data, ref offset, _tileId);
        BitCoderBuffer.SetGuid(data, ref offset, _edgeTypeMapId);

        this.WriteEdgesAndVerticesTo(data, ref offset);
        this.WriteAttributesTo(data, ref offset);
        this.WriteGeoTo(data, ref offset);
        this.WriteTurnCostsTo(data, ref offset);
    }

    internal int GetSerializedSizeUpperBound()
    {
        var size = 5 + 5 + 5 + 16;

        size += 5 + (int)_nextVertexId * 5;
        size += 5 + (int)_nextEdgeId;
        size += 5 + (int)_nextCrossTileId * 5;

        size += 5 + (int)_nextAttributePointer;
        size += 5;
        for (var i = 0; i < _nextStringId; i++)
        {
            size += 8 + System.Text.Encoding.Unicode.GetByteCount(_strings[i]);
        }

        size += 5;
        var coordinateSize = CoordinateSizeInBytes * 2;
        if (_elevation != null) coordinateSize += ElevationSizeInBytes;
        size += (int)(_nextVertexId * coordinateSize);
        size += 5 + (int)_nextShapePointer;

        size += 5 + _turnCostPointers.Length * 5;
        size += 5 + (int)_turnCostPointer;

        return size;
    }
}
