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
        _networkTile.WriteTo(stream);

        // write edges and vertices for boundary crossings.
        this.WriteEdgesAndVerticesTo(stream);
        
        // write attributes.
        this.WriteAttributesTo(stream);
    }

    private void WriteEdgesAndVerticesTo(Stream stream)
    {
        // write vertex pointers.
        stream.WriteVarUInt32(_crossingsPointer);
        for (var i = 0; i < _crossingsPointer; i++) {
            stream.WriteByte(_crossings[i]);
        }
    }

    public static StandaloneNetworkTile ReadFrom(Stream stream)
    {
        var version = stream.ReadVarInt32();
        if (version != 1) {
            throw new InvalidDataException("Cannot deserialize tiles: Invalid version #.");
        }

        var networkTile = NetworkTile.ReadFrom(stream);
        var standaloneNetworkTile = new StandaloneNetworkTile(networkTile);
        
        // read boundary crossings.
        standaloneNetworkTile.ReadEdgesAndVerticesFrom(stream);

        // read attributes.
        standaloneNetworkTile.ReadAttributesFrom(stream);
        
        return standaloneNetworkTile;
    }

    private void ReadEdgesAndVerticesFrom(Stream stream)
    {
        // read vertex pointers.
        _crossingsPointer = stream.ReadVarUInt32();
        _crossings.Resize(_crossingsPointer);
        for (var i = 0; i < _crossingsPointer; i++) {
            _crossings[i] = (byte)stream.ReadByte();
        }
    }
}