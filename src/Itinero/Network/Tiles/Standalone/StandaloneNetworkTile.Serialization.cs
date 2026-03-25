using System;
using System.IO;
using Itinero.IO;

namespace Itinero.Network.Tiles.Standalone;

public partial class StandaloneNetworkTile
{
    public void WriteTo(Stream stream)
    {
        var version = 2;
        stream.WriteVarInt32(version);

        // write base tile.
        this.NetworkTile.WriteTo(stream);

        // write edges and vertices for boundary crossings.
        this.WriteEdgesAndVerticesTo(stream);

        // write attributes.
        this.WriteAttributesTo(stream);

        // write global ids.
        this.WriteGlobal(stream);
    }

    private void WriteEdgesAndVerticesTo(Stream stream)
    {
        stream.WriteVarUInt32(_crossingsPointer);
        for (var i = 0; i < _crossingsPointer; i++)
        {
            stream.WriteByte(_crossings[i]);
        }
    }

    public static StandaloneNetworkTile ReadFrom(Stream stream)
    {
        var version = stream.ReadVarInt32();
        if (version != 2)
        {
            throw new InvalidDataException("Cannot deserialize tiles: Invalid version #.");
        }

        var networkTile = NetworkTile.ReadFrom(stream);
        var standaloneNetworkTile = new StandaloneNetworkTile(networkTile);

        // read boundary crossings.
        standaloneNetworkTile.ReadEdgesAndVerticesFrom(stream);

        // read attributes.
        standaloneNetworkTile.ReadAttributesFrom(stream);

        // read global.
        standaloneNetworkTile.ReadGlobal(stream);

        return standaloneNetworkTile;
    }

    private void ReadEdgesAndVerticesFrom(Stream stream)
    {
        // read vertex pointers.
        _crossingsPointer = stream.ReadVarUInt32();
        _crossings = new byte[_crossingsPointer];
        for (var i = 0; i < _crossingsPointer; i++)
        {
            _crossings[i] = (byte)stream.ReadByte();
        }
    }

    public static StandaloneNetworkTile ReadFrom(byte[] data, int offset)
    {
        var version = BitCoderBuffer.GetVarInt32(data, ref offset);
        if (version != 2)
        {
            throw new InvalidDataException("Cannot deserialize tiles: Invalid version #.");
        }

        var networkTile = NetworkTile.ReadFromBuffer(data, ref offset);
        var standaloneNetworkTile = new StandaloneNetworkTile(networkTile);

        standaloneNetworkTile.ReadEdgesAndVerticesFrom(data, ref offset);
        standaloneNetworkTile.ReadAttributesFrom(data, ref offset);
        standaloneNetworkTile.ReadGlobal(data, ref offset);

        return standaloneNetworkTile;
    }

    private void ReadEdgesAndVerticesFrom(byte[] data, ref int offset)
    {
        _crossingsPointer = BitCoderBuffer.GetVarUInt32(data, ref offset);
        _crossings = new byte[_crossingsPointer];
        Buffer.BlockCopy(data, offset, _crossings, 0, (int)_crossingsPointer);
        offset += (int)_crossingsPointer;
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

    private void WriteToBuffer(byte[] data, ref int offset)
    {
        const int version = 2;
        BitCoderBuffer.SetVarInt32(data, ref offset, version);

        this.NetworkTile.WriteToBuffer(data, ref offset);
        this.WriteEdgesAndVerticesTo(data, ref offset);
        this.WriteAttributesTo(data, ref offset);
        this.WriteGlobal(data, ref offset);
    }

    private void WriteEdgesAndVerticesTo(byte[] data, ref int offset)
    {
        BitCoderBuffer.SetVarUInt32(data, ref offset, _crossingsPointer);
        Buffer.BlockCopy(_crossings, 0, data, offset, (int)_crossingsPointer);
        offset += (int)_crossingsPointer;
    }

    private int GetSerializedSizeUpperBound()
    {
        var size = 5;
        size += this.NetworkTile.GetSerializedSizeUpperBound();

        size += 5 + (int)_crossingsPointer;

        size += 5 + (int)_nextAttributePointer;
        size += 5;
        for (var i = 0; i < _nextStringId; i++)
        {
            size += 8 + System.Text.Encoding.Unicode.GetByteCount(_strings[i]);
        }

        size += 5 + (int)_globalRestrictionsPointer;

        return size;
    }
}
