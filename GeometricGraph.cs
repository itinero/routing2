using System.Collections.Generic;
using Reminiscence;

namespace routing2
{
    /// <summary>
    /// A geometric graph.
    /// </summary>
    public class GeometricGraph
    {
        private readonly Dictionary<uint, VertexTile> _tiles;
        private readonly uint _zoom;

        /// <summary>
        /// Creates a new geometric graph.
        /// </summary>
        public GeometricGraph(uint zoom = 15)
        {
            _zoom = zoom;
            _tiles = new Dictionary<uint, VertexTile>();
        }

        /// <summary>
        /// Adds a new vertex.
        /// </summary>
        public ulong AddVertex(float latitude, float longitude)
        {
            var tileId = (uint)Tiles.Tile.WorldToTileIndex(latitude, longitude, _zoom).LocalId;

            VertexTile tile;
            if (!_tiles.TryGetValue(tileId, out tile))
            {
                tile = new VertexTile(tileId);
                _tiles[tileId] = tile;
            }

            var localId = tile.AddVertex(latitude, longitude);
            
            return VertexTile.BuildGlobalId(tileId, localId);            
        }

        /// <summary>
        /// Adds a new edge.
        /// </summary>
        /// <param name="vertex1"></param>
        /// <param name="vertex2"></param>
        /// <param name="data"></param>
        public void AddEdge(ulong vertex1, ulong vertex2, uint[] data)
        {
            
        }
    }
}