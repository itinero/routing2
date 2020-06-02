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

        internal void VertexTouched(RouterDbInstance routerDbInstance, VertexId vertexId)
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

                // download the tile.
                var tile = Tile.FromLocalId(vertexId.TileId, (int)_zoom);
                var url = _baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";
                using var stream = TileParser.DownloadFunc(url);
                
                // parse the tile.
                var parse = stream?.Parse(tile);
                if (parse == null)
                {
                    return;
                }

                // add the data from the tile.
                using (var routerDbInstanceWriter = routerDbInstance.GetWriter())
                {
                    routerDbInstanceWriter.AddOsmTile(_idMap, tile, parse);
                }
                _loadedTiles.Add(vertexId.TileId);
            }
        }

        internal void TouchBox(RouterDbInstance routerDbInstance, ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            // build the tile range.
            var tileRange = new TileRange(box, (int)_zoom);

            Parallel.ForEach(tileRange, (tile) =>
            {
                if (_loadedTiles.Contains(tile.LocalId)) return;

                var url = _baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";

                using var stream = TileParser.DownloadFunc(url);

                var parse = stream?.Parse(tile);
                if (parse == null)
                {
                    return;
                }

                lock (_loadedTiles)
                {
                    // add the data from the tile.
                    using (var routerDbInstanceWriter = routerDbInstance.GetWriter())
                    {
                        routerDbInstanceWriter.AddOsmTile(_idMap, tile, parse);
                    }

                    // mark tile as loaded.
                    _loadedTiles.Add(tile.LocalId);
                }
            });
        }
    }
}