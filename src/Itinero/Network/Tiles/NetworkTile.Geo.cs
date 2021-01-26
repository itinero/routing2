using System;
using System.Collections.Generic;
using System.IO;
using Itinero.IO;
using Itinero.Network.Storage;
using Reminiscence.Arrays;

namespace Itinero.Network.Tiles {
    internal partial class NetworkTile {
        private const int CoordinateSizeInBytes = 3; // 3 bytes = 24 bits = 4096 x 4096.
        private const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
        private const int ElevationSizeInBytes = 2; // 2 bytes = 16 bits = [-32768, 32767], using dm as resolution

        // the vertex coordinates.
        private readonly ArrayBase<byte> _coordinates;
        private int? _elevation; // the tile elevation.

        // the shapes.
        private uint _nextShapePointer = 0;
        private readonly ArrayBase<byte> _shapes;

        private void SetCoordinate(uint localId, double longitude, double latitude, float? e) {
            // set elevation if needed.
            if (_elevation != null && e == null) {
                throw new ArgumentNullException(nameof(e),
                    "Elevation was set before, either always set elevation or never set elevation.");
            }

            if (_elevation == null && e != null) {
                if (localId != 0) {
                    throw new ArgumentNullException(nameof(e),
                        "Elevation was not set before, either always set elevation or never set elevation.");
                }

                _elevation = (int) (e * 10);
            }

            // make sure coordinates fit.
            uint tileCoordinatePointer;
            if (_elevation == null) {
                // don't store elevation.
                tileCoordinatePointer = localId * CoordinateSizeInBytes * 2;
                _coordinates.EnsureMinimumSize(tileCoordinatePointer + CoordinateSizeInBytes * 2,
                    DefaultSizeIncrease);
            }
            else {
                // store elevation.
                tileCoordinatePointer = localId * (CoordinateSizeInBytes * 2 + ElevationSizeInBytes);
                _coordinates.EnsureMinimumSize(tileCoordinatePointer + CoordinateSizeInBytes * 2 + ElevationSizeInBytes,
                    DefaultSizeIncrease);
            }

            // write coordinates.
            const int resolution = (1 << TileResolutionInBits) - 1;
            var (x, y) = TileStatic.ToLocalTileCoordinates(_zoom, _tileId, longitude, latitude, resolution);
            _coordinates.SetFixed(tileCoordinatePointer, CoordinateSizeInBytes, x);
            _coordinates.SetFixed(tileCoordinatePointer + CoordinateSizeInBytes, CoordinateSizeInBytes, y);

            // write elevation.
            if (_elevation != null) {
                if (e == null) {
                    throw new ArgumentNullException(nameof(e),
                        "Elevation was set before, either always set elevation or never set elevation.");
                }

                var offset = (int) (e.Value * 10) - _elevation.Value;
                _coordinates.SetFixed(tileCoordinatePointer + CoordinateSizeInBytes + CoordinateSizeInBytes,
                    ElevationSizeInBytes, offset);
            }
        }

        private void GetCoordinate(uint localId, out double longitude, out double latitude, out float? elevation) {
            var tileCoordinatePointer = _elevation == null
                ? localId * CoordinateSizeInBytes * 2
                : localId * (CoordinateSizeInBytes * 2 + ElevationSizeInBytes);

            const int resolution = (1 << TileResolutionInBits) - 1;
            _coordinates.GetFixed(tileCoordinatePointer, CoordinateSizeInBytes, out var x);
            _coordinates.GetFixed(tileCoordinatePointer + CoordinateSizeInBytes, CoordinateSizeInBytes, out var y);
            elevation = null;
            if (_elevation != null) {
                _coordinates.GetFixed(tileCoordinatePointer + CoordinateSizeInBytes + CoordinateSizeInBytes,
                    ElevationSizeInBytes, out var offset);
                elevation = (_elevation.Value + offset) / 10.0f;
            }

            TileStatic.FromLocalTileCoordinates(_zoom, _tileId, x, y, resolution, out longitude, out latitude);
        }

        private uint SetShape(IEnumerable<(double longitude, double latitude, float? e)> shape) {
            const int resolution = (1 << TileResolutionInBits) - 1;
            var originalPointer = _nextShapePointer;
            var blockPointer = originalPointer;
            var pointer = blockPointer + 1;

            // make sure there is space for the block pointer.
            _shapes.EnsureMinimumSize(blockPointer);

            var coordinateBlockSize = 8;
            if (_elevation != null) {
                coordinateBlockSize += 4;
            }

            using var enumerator = shape.GetEnumerator();
            var count = 0;
            (int x, int y, int? eOffset) previous = (int.MaxValue, int.MaxValue, null);
            while (enumerator.MoveNext()) {
                var current = enumerator.Current;
                var (x, y) =
                    TileStatic.ToLocalTileCoordinates(_zoom, _tileId, current.longitude, current.latitude, resolution);
                int? eOffset = null;
                var e = current.e ?? 0;
                if (_elevation != null) {
                    eOffset = (int) (e * 10) - _elevation.Value;
                }

                // make sure there is space for this coordinate.
                _shapes.EnsureMinimumSize(pointer + coordinateBlockSize);

                // store coordinate.
                if (count == 0) {
                    // first coordinate.
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, x);
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, y);
                    if (eOffset != null) {
                        pointer += (uint) _shapes.SetDynamicInt32(pointer, eOffset.Value);
                    }
                }
                else {
                    // calculate diff and then store.
                    var diffX = x - previous.x;
                    var diffY = y - previous.y;
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, diffX);
                    pointer += (uint) _shapes.SetDynamicInt32(pointer, diffY);
                    if (eOffset != null) {
                        if (previous.eOffset == null) {
                            throw new ArgumentException("Not all points have elevation set.");
                        }

                        var diffE = eOffset.Value - previous.eOffset.Value;
                        pointer += (uint) _shapes.SetDynamicInt32(pointer, diffE);
                    }
                }

                if (count == 255) {
                    // start a new block, assign 255.
                    _shapes[blockPointer] = 255;
                    blockPointer = pointer;
                    pointer = blockPointer + 1;
                    count = 0;
                }
                else {
                    count++;
                }

                previous = (x, y, eOffset);
            }

            // a block is still open, close it.
            _shapes[blockPointer] = (byte) count;
            _nextShapePointer = pointer;

            return originalPointer;
        }

        internal IEnumerable<(double longitude, double latitude, float? e)> GetShape(uint? pointer) {
            if (pointer == null) {
                yield break;
            }

            var p = pointer.Value;

            const int resolution = (1 << TileResolutionInBits) - 1;
            var count = -1;
            (int x, int y, int? eOffset) previous = (int.MaxValue, int.MaxValue, null);
            do {
                count = _shapes[p];
                p++;

                for (var i = 0; i < count; i++) {
                    p += (uint) _shapes.GetDynamicInt32(p, out var x);
                    p += (uint) _shapes.GetDynamicInt32(p, out var y);
                    int? eOffset = null;
                    if (_elevation != null) {
                        p += (uint) _shapes.GetDynamicInt32(p, out var e);
                        eOffset = e;
                    }

                    if (previous.x != int.MaxValue) {
                        x = previous.x + x;
                        y = previous.y + y;
                        if (_elevation != null) {
                            if (previous.eOffset == null) {
                                throw new ArgumentException("Not all points have elevation set.");
                            }

                            eOffset = previous.eOffset.Value + eOffset;
                        }
                    }

                    int? elevation = null;
                    if (_elevation != null) {
                        if (eOffset == null) {
                            throw new ArgumentException("Not all points have elevation set.");
                        }

                        elevation = _elevation.Value + eOffset.Value;
                    }

                    TileStatic.FromLocalTileCoordinates(_zoom, _tileId, x, y, resolution, out var longitude,
                        out var latitude);
                    yield return (longitude, latitude, elevation / 10.0f);

                    previous = (x, y, eOffset);
                }
            } while (count == 255);
        }

        private void WriteGeoTo(Stream stream) {
            stream.WriteVarInt32Nullable(_elevation);

            // write vertex locations.
            var coordinateSize = CoordinateSizeInBytes * 2;
            if (_elevation != null) {
                coordinateSize += ElevationSizeInBytes;
            }

            var coordinateBytes = _nextVertexId * coordinateSize;
            for (var i = 0; i < coordinateBytes; i++) {
                stream.WriteByte(_coordinates[i]);
            }

            // write shape locations.
            stream.WriteVarUInt32(_nextShapePointer);
            for (var i = 0; i < _nextShapePointer; i++) {
                stream.WriteByte(_shapes[i]);
            }
        }

        private void ReadGeoFrom(Stream stream) {
            _elevation = stream.ReadVarInt32Nullable();

            // read vertex locations.
            var coordinateSize = CoordinateSizeInBytes * 2;
            if (_elevation != null) {
                coordinateSize += ElevationSizeInBytes;
            }

            var coordinateBytes = _nextVertexId * coordinateSize;
            _coordinates.Resize(coordinateBytes);
            for (var i = 0; i < coordinateBytes; i++) {
                _coordinates[i] = (byte) stream.ReadByte();
            }

            _nextShapePointer = stream.ReadVarUInt32();
            _shapes.Resize(_nextShapePointer);
            for (var i = 0; i < _nextShapePointer; i++) {
                _shapes[i] = (byte) stream.ReadByte();
            }
        }
    }
}