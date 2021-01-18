using System.IO;
using Itinero.IO;

namespace Itinero.Network.Tiles
{
    internal partial class NetworkTile
    {
        public void WriteTo(Stream stream)
        {
            var version = 1;
            stream.WriteVarInt32(version);
            
            // write tileid/zoom.
            stream.WriteVarInt32(_zoom);
            stream.WriteVarUInt32(_tileId);
            
            // write vertices and edges.
            this.SerializeEdgesAndVertices(stream);
            
            // write attributes.
            this.SerializeAttributes(stream);
            
            // write shapes.
            this.SerializeShapes(stream);
        }

        public static NetworkTile ReadFrom(Stream stream)
        {
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Cannot deserialize tiles: Invalid version #.");
            
            // read tile id.
            var zoom = stream.ReadVarInt32();
            var tileId = stream.ReadVarUInt32();
            
            // create the tile.
            var graphTile = new NetworkTile(zoom, tileId);
            
            // read vertices and edges.
            graphTile.DeserializeEdgesAndVertices(stream);
            
            // read attributes.
            graphTile.DeserializeAttributes(stream);
            
            // read shapes.
            graphTile.DeserializeShapes(stream);

            return graphTile;
        }
    }
}