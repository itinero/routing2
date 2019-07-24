using System.Collections.Generic;
using System.IO;
using Itinero.Data.Graphs;
using Itinero.Data.Providers;
using Itinero.IO.Osm.Tiles.Parsers;
using Itinero.Profiles;
using Reminiscence;
using Reminiscence.Arrays;

namespace Itinero.IO.Osm.Tiles
{
    /// <summary>
    /// Represents a data provider.
    /// </summary>
    public class DataProvider : ILiveDataProvider
    {
        private RouterDb _routerDb;
        private GlobalIdMap _idMap;
        private readonly string _baseUrl;
        private readonly HashSet<uint> _loadedTiles;
        private readonly int _zoom;

        /// <summary>
        /// Creates a new data provider.
        /// </summary>
        /// <param name="baseUrl">The base url to load tiles from.</param>
        /// <param name="globalIdMap">The global id map, if any.</param>
        /// <param name="zoom">The zoom level.</param>
        public DataProvider(string baseUrl = TileParser.BaseUrl,
            GlobalIdMap globalIdMap = null, int zoom = 14)
        {
            _idMap = globalIdMap ?? new GlobalIdMap();
            _baseUrl = baseUrl;
            _zoom = 14;
            
            _loadedTiles = new HashSet<uint>();
        }

        /// <inheritdoc/>
        public void SetRouterDb(RouterDb routerDb)
        {
            _routerDb = routerDb;
        }

        /// <inheritdoc/>
        public bool TouchVertex(VertexId vertexId)
        {
            if (_loadedTiles.Contains(vertexId.TileId))
            { // tile was already loaded.
                return false;
            }

            lock (_loadedTiles)
            {
                if (_loadedTiles.Contains(vertexId.TileId))
                { // tile was already loaded.
                    return false;
                }
                
                var result = _routerDb.AddOsmTile(_idMap, Tile.FromLocalId(vertexId.TileId, _zoom), _baseUrl);
                _loadedTiles.Add(vertexId.TileId);
                return result;
            }
        }

        /// <inheritdoc/>
        public bool TouchBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            // build the tile range.
            var tileRange = new TileRange(box, _zoom);
            
            // get all the tiles and build the router db.
            var updated = false;
            foreach (var tile in tileRange)
            {
                if (_loadedTiles.Contains(tile.LocalId)) continue;


                lock (_loadedTiles)
                {
                    if (_routerDb.AddOsmTile(_idMap, tile, _baseUrl))
                    {
                        updated = true;
                    }
                }

                _loadedTiles.Add(tile.LocalId);
            }

            return updated;
        }

        /// <inheritdoc/>
        public long WriteTo(Stream stream)
        {
            lock (_loadedTiles)
            {
                var p = stream.Position;
            
                // write header and version.
                stream.WriteWithSize($"{nameof(DataProvider)}");
                stream.WriteByte(1);
            
                // write loaded tiles.
                var tilesArray = new MemoryArray<uint>(_loadedTiles.Count);
                var t = 0;
                foreach (var loadedTile in _loadedTiles)
                {
                    tilesArray[t] = loadedTile;
                    t++;
                }
                tilesArray.CopyToWithSize(stream);
                
                // write global id map.
                _idMap.WriteTo(stream);

                return stream.Position - p;
            }
        }

        /// <inheritdoc/>
        public void ReadFrom(Stream stream)
        {
            // read & verify header.
            var header = stream.ReadWithSizeString();
            var version = stream.ReadByte();
            if (header != nameof(DataProvider)) throw new InvalidDataException($"Cannot read {nameof(DataProvider)}: Header invalid.");
            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(DataProvider)}: Version # invalid.");
            
            // read load tiles.
            var tilesArray = MemoryArray<uint>.CopyFromWithSize(stream);
            
            // read global id map.
            var globalIdMap = GlobalIdMap.ReadFrom(stream);

            lock (_loadedTiles)
            {
                _idMap = globalIdMap;
                for (var t = 0; t < tilesArray.Length; t++)
                {
                    _loadedTiles.Add(tilesArray[t]);
                }
            }
        }
    }
}