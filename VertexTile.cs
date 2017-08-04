using Reminiscence.Arrays;
using System.IO;
using System;

namespace routing2
{
    /// <summary>
    /// Represents one individual tile.
    /// </summary>
    public class VertexTile
    {
        private const int BLOCK_SIZE = 1024;
        private const uint NO_DATA = uint.MaxValue;

        private readonly ArrayBase<uint> _data; // the base data: [ref-to-edges]
        private readonly ArrayBase<float> _coordinates;
        private readonly ArrayBase<uint> _edges; // the edges data.
        private readonly ShapesArray _shapes;

        /// <summary>
        /// Creates a new vertex tile.
        /// </summary>
        public VertexTile(uint id)
        {
            this.Id = id;

            _data = new MemoryArray<uint>(BLOCK_SIZE);
            _coordinates = new MemoryArray<float>(BLOCK_SIZE);
            _edges = new MemoryArray<uint>(BLOCK_SIZE);
            _shapes = new ShapesArray(BLOCK_SIZE);
        }

        private uint _nextId = 0;

        /// <summary>
        /// Gets or sets tile id.
        /// </summary>
        /// <returns></returns>
        public uint Id { get; private set; } 

        /// <summary>
        /// Adds a new vertex, returns a local id.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public uint AddVertex(float latitude, float longitude)
        {
            var id = _nextId;
            _nextId++;

            _data.EnsureMinimumSize(id);
            _coordinates.EnsureMinimumSize(id * 2);

            _data[id] = NO_DATA;
            _coordinates[id * 2 + 0] = latitude;
            _coordinates[id * 2 + 1] = longitude;

            return id;
        }

        /// <summary>
        /// Serializes this tile to the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public long Serialize(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserializes a tile from the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static VertexTile Deserialize(Stream stream, VertexTileProfile profile = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Builds a local id.
        /// </summary>
        /// <param name="tileId"></param>
        /// <param name="localId"></param>
        /// <returns></returns>
        public static ulong BuildGlobalId(uint tileId, uint localId)
        {
            return 0;
        }

        /// <summary>
        /// Extracts a local id.
        /// </summary>
        /// <param name="globalId"></param>
        /// <param name="tileId"></param>
        /// <param name="localId"></param>
        public static void ExtractLocalId(ulong globalId, out uint tileId, out uint localId)
        {
            tileId = 0;
            localId = 0;
        }
    }
}