using System.Collections.Generic;
using System.Threading.Tasks;
using Itinero.Data.Graphs;
using Itinero.Data.Providers;
using Itinero.IO.Osm.Tiles.Parsers;

namespace Itinero.IO.Osm.Tiles
{
    /// <summary>
    /// Represents a data provider.
    /// </summary>
    public class DataProvider : ILiveDataProvider
    {
        private readonly RouterDb _routerDb;
        private readonly GlobalIdMap _idMap;
        private readonly string _baseUrl;
        private readonly HashSet<uint> _loadedTiles;
        private readonly int _zoom;

        /// <summary>
        /// Creates a new data provider.
        /// </summary>
        /// <param name="routerDb">The router db to load data in.</param>
        /// <param name="baseUrl">The base url to load tiles from.</param>
        /// <param name="globalIdMap">The global id map, if any.</param>
        /// <param name="zoom">The zoom level.</param>
        public DataProvider(RouterDb routerDb, string baseUrl = TileParser.BaseUrl,
            GlobalIdMap globalIdMap = null, int zoom = 14)
        {
            _routerDb = routerDb;
            _idMap = globalIdMap ?? new GlobalIdMap();
            _baseUrl = baseUrl;
            _zoom = 14;
            
            _loadedTiles = new HashSet<uint>();
        }

        /// <summary>
        /// Clones the data provider with the same state but with a new router db.
        /// </summary>
        /// <param name="routerDb"></param>
        /// <returns></returns>
        public DataProvider CloneFor(RouterDb routerDb)
        {
            var dp = new DataProvider(routerDb, _baseUrl, _idMap, _zoom);

            foreach (var tile in this._loadedTiles)
            {
                dp._loadedTiles.Add(tile);
            }

            return dp;
        }

        /// <inheritdoc/>
        public bool TouchVertex(VertexId vertexId)
        {
            if (_loadedTiles.Contains(vertexId.TileId))
            {
                // tile was already loaded.
                return false;
            }

            lock (_loadedTiles)
            {
                if (_loadedTiles.Contains(vertexId.TileId))
                {
                    // tile was already loaded.
                    return false;
                }

                var tile = Tile.FromLocalId(vertexId.TileId, _zoom);
                var url = _baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";
                using (var stream = TileParser.DownloadFunc(url))
                {
                    var parse = stream?.Parse(tile);
                    if (parse == null)
                    {
                        return false;
                    }

                    var result = _routerDb.AddOsmTile(_idMap, tile, parse);
                    _loadedTiles.Add(vertexId.TileId);
                    return result;
                }
            }
        }

        /// <inheritdoc/>
        public bool TouchBox((double minLon, double minLat, double maxLon, double maxLat) box)
        {
            // build the tile range.
            var tileRange = new TileRange(box, _zoom);
            
            // get all the tiles and build the router db.
            var updated = false;

            Parallel.ForEach(tileRange, (tile) =>
                //foreach (var tile in tileRange)
            {
                if (_loadedTiles.Contains(tile.LocalId)) return;

                var url = _baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";

                using (var stream = TileParser.DownloadFunc(url))
                {
                    var parse = stream?.Parse(tile);
                    if (parse == null)
                    {
                        return;
                    }

                    lock (_loadedTiles)
                    {
                        if (_routerDb.AddOsmTile(_idMap, tile, parse))
                        {
                            updated = true;
                        }

                        _loadedTiles.Add(tile.LocalId);
                    }
                }
            });

            return updated;
        }
    }
}