using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Data.Usage;
using Itinero.IO.Osm.Tiles.Parsers;
using Itinero.Network;

namespace Itinero.IO.Osm.Tiles;

/// <summary>
/// A data provider loading routable tiles on demand.
/// </summary>
internal class DataProvider : IDataUseListener
{
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
        _baseUrl = baseUrl;
        _zoom = 14;

        _loadedTiles = new HashSet<uint>();
        _idMap = new GlobalIdMap();

        // get notified when a location/area is used.
        routerDb.Latest.UsageNotifier.AddListener(this);
    }

    private DataProvider(RouterDb routerDb, string baseUrl, uint zoom,
        HashSet<uint> loadedTiles, GlobalIdMap idMap)
    {
        _baseUrl = baseUrl;
        _zoom = zoom;
        _idMap = idMap;
        _loadedTiles = loadedTiles;

        // get notified when a location/area is used.
        routerDb.Latest.UsageNotifier.AddListener(this);
    }

    IDataUseListener? IDataUseListener.CloneForNewNetwork(RoutingNetwork routingNetwork)
    {
        return null;
    }

    Task IDataUseListener.VertexTouched(RoutingNetwork network, VertexId vertexId, CancellationToken cancellationToken)
    {
        if (_loadedTiles.Contains(vertexId.TileId))
        {
            return Task.CompletedTask;
        }

        lock (_loadedTiles)
        {
            if (_loadedTiles.Contains(vertexId.TileId))
            {
                return Task.CompletedTask;
            }

            // download the tile.
            var tile = Tile.FromLocalId(vertexId.TileId, (int)_zoom);
            var url = _baseUrl + $"/{tile.Zoom}/{tile.X}/{tile.Y}";
            using var stream = TileParser.DownloadFunc(url);

            // parse the tile.
            var parse = stream?.Parse(tile);
            if (parse == null)
            {
                return Task.CompletedTask;
            }

            // add the data from the tile.
            using (var routerDbInstanceWriter = network.GetWriter())
            {
                routerDbInstanceWriter.AddOsmTile(_idMap, tile, parse);
            }

            _loadedTiles.Add(vertexId.TileId);
        }

        return Task.CompletedTask;
    }

    Task IDataUseListener.BoxTouched(RoutingNetwork network,
        ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
            bottomRight) box, CancellationToken cancellationToken)
    {
        // build the tile range.
        var tileRange = new TileRange(box, (int)_zoom);

        Parallel.ForEach(tileRange, (tile) =>
        {
            if (_loadedTiles.Contains(tile.LocalId))
            {
                return;
            }

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
                using (var routerDbInstanceWriter = network.GetWriter())
                {
                    routerDbInstanceWriter.AddOsmTile(_idMap, tile, parse);
                }

                // mark tile as loaded.
                _loadedTiles.Add(tile.LocalId);
            }
        });
        return Task.CompletedTask;
    }

    internal void WriteTo(Stream stream)
    {
        // write version #.
        stream.WriteWithSize($"{nameof(DataProvider)}");
        stream.WriteVarInt32(1);

        // write details.
        stream.WriteWithSize(_baseUrl);
        stream.WriteVarUInt32(_zoom);

        // write global id map.
        lock (_loadedTiles)
        {
            _idMap.WriteTo(stream);

            // write loaded tiles.
            stream.WriteVarInt64(_loadedTiles.Count);
            foreach (var tileId in _loadedTiles)
            {
                stream.WriteVarUInt32(tileId);
            }
        }
    }

    internal static DataProvider ReadFrom(Stream stream, RouterDb routerDb)
    {
        // read & verify header.
        var header = stream.ReadWithSizeString();
        var version = stream.ReadVarInt32();
        if (header != nameof(DataProvider))
        {
            throw new InvalidDataException($"Cannot read {nameof(DataProvider)}: Header invalid.");
        }

        if (version != 1)
        {
            throw new InvalidDataException($"Cannot read {nameof(DataProvider)}: Version # invalid.");
        }

        // read details.
        var baseUrl = stream.ReadWithSizeString();
        var zoom = stream.ReadVarUInt32();

        // write global id map.
        var idMap = GlobalIdMap.ReadFrom(stream);

        // read loaded tiles.
        var loadedTileCount = stream.ReadVarInt64();
        var loadedTiles = new HashSet<uint>();
        for (var t = 0; t < loadedTileCount; t++)
        {
            loadedTiles.Add(stream.ReadVarUInt32());
        }

        return new DataProvider(routerDb, baseUrl, zoom, loadedTiles, idMap);
    }
}
