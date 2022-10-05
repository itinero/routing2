using System.IO;
using Itinero.IO;

namespace Itinero.Network.Tiles.Standalone;

public partial class StandaloneNetworkTile
{
    public void WriteTo(Stream stream)
    {
        var version = 1;
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

    private void WriteGlobal(Stream stream)
    {
        stream.WriteVarUInt32(_globalIdPointer);
        for (var i = 0; i < _globalIdPointer; i++)
        {
            stream.WriteByte(_globalIds[i]);
        }

        stream.WriteVarUInt32(_turnCostPointer);
        for (var i = 0; i < _turnCostPointer; i++)
        {
            stream.WriteByte(_turnCosts[i]);
        }
    }

    public static StandaloneNetworkTile ReadFrom(Stream stream)
    {
        var version = stream.ReadVarInt32();
        if (version != 1)
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
        _crossings.Resize(_crossingsPointer);
        for (var i = 0; i < _crossingsPointer; i++)
        {
            _crossings[i] = (byte)stream.ReadByte();
        }
    }

    private void ReadGlobal(Stream stream)
    {
        _globalIdPointer = stream.ReadVarUInt32();
        _globalIds.Resize(_globalIdPointer);
        for (var i = 0; i < _globalIdPointer; i++)
        {
            _globalIds[i] = (byte)stream.ReadByte();
        }

        _turnCostPointer = stream.ReadVarUInt32();
        _turnCosts.Resize(_turnCostPointer);
        for (var i = 0; i < _turnCostPointer; i++)
        {
            _turnCosts[i] = (byte)stream.ReadByte();
        }
    }
}
