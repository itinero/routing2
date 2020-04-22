using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Itinero.Data.Graphs;
using Itinero.IO.Osm.Tiles.Parsers;

namespace Itinero.IO.Osm.Tiles
{
    /// <summary>
    /// A data provider loading routable tiles on demand.
    /// </summary>
    internal class DataProvider
    {
        private readonly RouterDb _routerDb;
        private readonly GlobalIdMap _idMap;
        private readonly string _baseUrl;
        private readonly HashSet<uint> _loadedTiles;
        private readonly uint _zoom;

        /// <summary>
        /// Creates a new data provider.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="baseUrl">The base url to load tiles from.</param>
        /// <param name="zoom">The zoom level.</param>
        internal DataProvider(RouterDb routerDb, string baseUrl = TileParser.BaseUrl, uint zoom = 14)
        {
            _routerDb = routerDb;
            _baseUrl = baseUrl;
            _zoom = 14;
            
            _loadedTiles = new HashSet<uint>();
            _idMap = new GlobalIdMap();

            _routerDb.UsageNotifier.OnVertexTouched += VertexTouched;
            _routerDb.UsageNotifier.OnBoxTouched += TouchBox;
        }

        internal void VertexTouched(VertexId vertexId)
        {
            if (_loadedTiles.Contains(vertexId.TileId))
            {
                // tile was already loaded.
                return;
            }

            lock (_loadedTiles)
            {
                if (_loadedTiles.Contains(vertexId.TileId))
                {
                    // tile was already loaded.
                    return;
                }

                var tile = Tile.FromLocalId(vertexId.TileId, (int)_zoom);
                var url = _baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";
                using var stream = TileParser.DownloadFunc(url);
                
                var parse = stream?.Parse(tile);
                if (parse == null)
                {
                    return;
                }

                var result = _routerDb.AddOsmTile(_idMap, tile, parse);
                _loadedTiles.Add(vertexId.TileId);
            }
        }

        internal void TouchBox(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            // build the tile range.
            var tileRange = new TileRange(box, (int)_zoom);

//            Parallel.ForEach(tileRange, (tile) =>
//            {
            foreach (var tile in tileRange)
            {
                if (_loadedTiles.Contains(tile.LocalId)) return;

                var url = _baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";

                using var stream = TileParser.DownloadFunc(url);

                var parse = stream?.Parse(tile);
                if (parse == null)
                {
                    continue;
                }

                lock (_loadedTiles)
                {
                    _routerDb.AddOsmTile(_idMap, tile, parse);

                    _loadedTiles.Add(tile.LocalId);
                }
            }

            //});
        }

//        /// <inheritdoc/>
//        public long WriteTo(Stream stream)
//        {
//            lock (_loadedTiles)
//            {
//                var p = stream.Position;
//            
//                // write header and version.
//                stream.WriteWithSize($"{nameof(DataProvider)}");
//                stream.WriteByte(1);
//            
//                // write loaded tiles.
//                var tilesArray = new MemoryArray<uint>(_loadedTiles.Count);
//                var t = 0;
//                foreach (var loadedTile in _loadedTiles)
//                {
//                    tilesArray[t] = loadedTile;
//                    t++;
//                }
//                tilesArray.CopyToWithSize(stream);
//                
//                // write global id map.
//                _idMap.WriteTo(stream);
//
//                return stream.Position - p;
//            }
//        }
//
//        /// <inheritdoc/>
//        public void ReadFrom(Stream stream)
//        {
//            // read & verify header.
//            var header = stream.ReadWithSizeString();
//            var version = stream.ReadByte();
//            if (header != nameof(DataProvider)) throw new InvalidDataException($"Cannot read {nameof(DataProvider)}: Header invalid.");
//            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(DataProvider)}: Version # invalid.");
//            
//            // read load tiles.
//            var tilesArray = MemoryArray<uint>.CopyFromWithSize(stream);
//            
//            // read global id map.
//            var globalIdMap = GlobalIdMap.ReadFrom(stream);
//
//            lock (_loadedTiles)
//            {
//                _idMap = globalIdMap;
//                for (var t = 0; t < tilesArray.Length; t++)
//                {
//                    _loadedTiles.Add(tilesArray[t]);
//                }
//            }
//        }
    }
}