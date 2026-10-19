using System;
using System.Collections.Generic;
using System.IO;
using Itinero.IO;
using Itinero.Network.Storage;

namespace Itinero.Network.Tiles;

internal partial class NetworkTile
{
    private const int CoordinateSizeInBytes = 3; // 3 bytes = 24 bits = 4096 x 4096.
    private const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
    private const int ElevationSizeInBytes = 2; // 2 bytes = 16 bits = [-32768, 32767], using dm as resolution

    // the vertex coordinates.
    private byte[] _coordinates;
    private int? _elevation; // the tile elevation.

    // the shapes.

    private void SetCoordinate(uint localId, double longitude, double latitude, float? e)
    {
        // set elevation if needed.
        if (_elevation != null && e == null)
        {
            throw new ArgumentNullException(nameof(e),
                "Elevation was set before, either always set elevation or never set elevation.");
        }

        if (_elevation == null && e != null)
        {
            if (localId != 0)
            {
                throw new ArgumentNullException(nameof(e),
                    "Elevation was not set before, either always set elevation or never set elevation.");
            }

            _elevation = (int)(e * 10);
        }

        // make sure coordinates fit.
        uint tileCoordinatePointer;
        if (_elevation == null)
        {
            // don't store elevation.
            tileCoordinatePointer = localId * CoordinateSizeInBytes * 2;
            ArrayBaseExtensions.EnsureMinimumSize(ref _coordinates, tileCoordinatePointer + (CoordinateSizeInBytes * 2),
                DefaultSizeIncrease);
        }
        else
        {
            // store elevation.
            tileCoordinatePointer = localId * ((CoordinateSizeInBytes * 2) + ElevationSizeInBytes);
            ArrayBaseExtensions.EnsureMinimumSize(ref _coordinates, tileCoordinatePointer + (CoordinateSizeInBytes * 2) + ElevationSizeInBytes,
                DefaultSizeIncrease);
        }

        // write coordinates.
        var (x, y) = this.ToLocalCoordinates(longitude, latitude);
        _coordinates.SetFixed(tileCoordinatePointer, CoordinateSizeInBytes, x);
        _coordinates.SetFixed(tileCoordinatePointer + CoordinateSizeInBytes, CoordinateSizeInBytes, y);

        // write elevation.
        if (_elevation != null)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e),
                    "Elevation was set before, either always set elevation or never set elevation.");
            }

            var offset = (int)(e.Value * 10) - _elevation.Value;
            _coordinates.SetFixed(tileCoordinatePointer + CoordinateSizeInBytes + CoordinateSizeInBytes,
                ElevationSizeInBytes, offset);
        }
    }

    private void GetCoordinate(uint localId, out double longitude, out double latitude, out float? elevation)
    {
        var tileCoordinatePointer = _elevation == null
            ? localId * CoordinateSizeInBytes * 2
            : localId * ((CoordinateSizeInBytes * 2) + ElevationSizeInBytes);

        const int resolution = (1 << TileResolutionInBits) - 1;
        _coordinates.GetFixed(tileCoordinatePointer, CoordinateSizeInBytes, out var x);
        _coordinates.GetFixed(tileCoordinatePointer + CoordinateSizeInBytes, CoordinateSizeInBytes, out var y);
        elevation = null;
        if (_elevation != null)
        {
            _coordinates.GetFixed(tileCoordinatePointer + CoordinateSizeInBytes + CoordinateSizeInBytes,
                ElevationSizeInBytes, out var offset);
            elevation = (_elevation.Value + offset) / 10.0f;
        }

        this.FromLocalCoordinates(x, y, out longitude, out latitude);
    }

    // Box and step size are the same for every vertex in the tile; computing them per
    // coordinate read cost two Math.Sinh, two Math.Atan and four Math.Pow each time.
    private readonly double _boxLeft;

    private readonly double _boxTop;

    private readonly double _boxLonStep;

    private readonly double _boxLatStep;

    /// The local-coordinate resolution every call site uses; a compile-time constant.
    private const int CoordinateResolution = (1 << TileResolutionInBits) - 1;

    private (double left, double top, double lonStep, double latStep) ComputeBox()
    {
        var (minLon, minLat, maxLon, maxLat) = TileStatic.GetTileBoundingBox(_zoom, _tileId);

        return (minLon, maxLat,
            (maxLon - minLon) / CoordinateResolution,
            (maxLat - minLat) / CoordinateResolution);
    }

    /// Tile-local integer coordinates to lon/lat, using the cached box.
    private void FromLocalCoordinates(int x, int y, out double longitude, out double latitude)
    {
        longitude = _boxLeft + (_boxLonStep * x);
        latitude = _boxTop - (_boxLatStep * y);
    }

    /// Lon/lat to tile-local integer coordinates, using the cached box.
    private (int x, int y) ToLocalCoordinates(double longitude, double latitude) =>
        ((int)((longitude - _boxLeft) / _boxLonStep), (int)((_boxTop - latitude) / _boxLatStep));


    private void WriteGeoTo(Stream stream)
    {
        stream.WriteVarInt32Nullable(_elevation);

        // write vertex locations.
        var coordinateSize = CoordinateSizeInBytes * 2;
        if (_elevation != null)
        {
            coordinateSize += ElevationSizeInBytes;
        }

        var coordinateBytes = _nextVertexId * coordinateSize;
        for (var i = 0; i < coordinateBytes; i++)
        {
            stream.WriteByte(_coordinates[i]);
        }
    }

    private void ReadGeoFrom(Stream stream)
    {
        _elevation = stream.ReadVarInt32Nullable();

        // read vertex locations.
        var coordinateSize = CoordinateSizeInBytes * 2;
        if (_elevation != null)
        {
            coordinateSize += ElevationSizeInBytes;
        }

        var coordinateBytes = _nextVertexId * coordinateSize;
        Array.Resize(ref _coordinates, (int)coordinateBytes);
        for (var i = 0; i < coordinateBytes; i++)
        {
            _coordinates[i] = (byte)stream.ReadByte();
        }
    }

    private void ReadGeoFrom(byte[] data, ref int offset)
    {
        _elevation = BitCoderBuffer.GetVarInt32Nullable(data, ref offset);

        var coordinateSize = CoordinateSizeInBytes * 2;
        if (_elevation != null)
        {
            coordinateSize += ElevationSizeInBytes;
        }

        var coordinateBytes = (int)(_nextVertexId * coordinateSize);
        Array.Resize(ref _coordinates, coordinateBytes);
        Buffer.BlockCopy(data, offset, _coordinates, 0, coordinateBytes);
        offset += coordinateBytes;
    }

    private void WriteGeoTo(byte[] data, ref int offset)
    {
        BitCoderBuffer.SetVarInt32Nullable(data, ref offset, _elevation);

        var coordinateSize = CoordinateSizeInBytes * 2;
        if (_elevation != null)
        {
            coordinateSize += ElevationSizeInBytes;
        }

        var coordinateBytes = (int)(_nextVertexId * coordinateSize);
        Buffer.BlockCopy(_coordinates, 0, data, offset, coordinateBytes);
        offset += coordinateBytes;
    }
}
