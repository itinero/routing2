using System;
using System.Collections;
using System.Collections.Generic;

namespace Itinero.IO.Osm.Tiles {
    internal struct TileRange : IEnumerable<Tile> {
        public TileRange(
            ((double longitude, double latitude, float? e) topLeft, (double longitude, double latitude, float? e)
                bottomRight) box, int zoom) {
            var topLeft = Tile.WorldToTile(box.topLeft.longitude, box.topLeft.latitude, zoom);
            var bottomRight = Tile.WorldToTile(box.bottomRight.longitude, box.bottomRight.latitude, zoom);

            Left = topLeft.X;
            Top = topLeft.Y;
            Right = bottomRight.X;
            Bottom = bottomRight.Y;
            Zoom = zoom;

            if (Top > Bottom) {
                throw new ArgumentException("Invalid tile range, top is lower than bottom.");
            }
        }

        public uint Left { get; }

        public uint Right { get; }

        public uint Top { get; }

        public uint Bottom { get; }

        public int Zoom { get; }

        public IEnumerator<Tile> GetEnumerator() {
            return new TileRangeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private struct TileRangeEnumerator : IEnumerator<Tile> {
            private readonly TileRange _tileRange;

            public TileRangeEnumerator(TileRange tileRange) {
                _tileRange = tileRange;
                _x = uint.MaxValue;
                _y = uint.MaxValue;
            }

            private uint _x;
            private uint _y;

            public bool MoveNext() {
                if (_x == uint.MaxValue) {
                    _x = _tileRange.Left;
                    _y = _tileRange.Top;
                    return true;
                }

                if (_tileRange.Left > _tileRange.Right) { // not supported.
                    throw new Exception("Tile ranges crossing date line not supported.");
                }

                if (_x == _tileRange.Right) { // move y.
                    if (_y == _tileRange.Bottom) { // enumeration finished.
                        return false;
                    }

                    _y++;
                    _x = _tileRange.Left;
                    return true;
                }

                _x++;
                return true;
            }

            public void Reset() {
                _x = uint.MaxValue;
                _y = uint.MaxValue;
            }

            public Tile Current => new(_x, _y, _tileRange.Zoom);

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}