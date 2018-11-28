/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Itinero.IO.Osm.Tiles
{
    internal struct TileRange : IEnumerable<Tile>
    {
        public TileRange((double minLon, double minLat, double maxLon, double maxLat) box, int zoom)
        {
            var topLeft = Tile.WorldToTile(box.minLon, box.maxLat, zoom);
            var bottomRight = Tile.WorldToTile(box.maxLon, box.minLat, zoom);

            this.Left = topLeft.X;
            this.Top = topLeft.Y;
            this.Right = bottomRight.X;
            this.Bottom = bottomRight.Y;
            this.Zoom = zoom;

            if (this.Top > this.Bottom)
            {
                throw new ArgumentException("Invalid tile range, top is lower than bottom.");
            }
        }
        
        public uint Left { get; }
        
        public uint Right { get; }
        
        public uint Top { get; }
        
        public uint Bottom { get; }
        
        public int Zoom { get; }
        
        public IEnumerator<Tile> GetEnumerator()
        {
            return new TileRangeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private struct TileRangeEnumerator : IEnumerator<Tile>
        {
            private readonly TileRange _tileRange;

            public TileRangeEnumerator(TileRange tileRange)
            {
                _tileRange = tileRange;
                _x = uint.MaxValue;
                _y = uint.MaxValue;
            }

            private uint _x;
            private uint _y;

            public bool MoveNext()
            {
                if (_x == uint.MaxValue)
                {
                    _x = _tileRange.Left;
                    _y = _tileRange.Top;
                    return true;
                }

                if (_tileRange.Left > _tileRange.Right)
                { // not supported.
                    throw new Exception("Tile ranges crossing date line not supported.");
                }
                if (_x == _tileRange.Right)
                { // move y.
                    if (_y == _tileRange.Bottom)
                    { // enumeration finished.
                        return false;
                    }

                    _y++;
                    _x = _tileRange.Left;
                    return true;
                }

                _x++;
                return true;
            }

            public void Reset()
            {
                _x = uint.MaxValue;
                _y = uint.MaxValue;
            }

            public Tile Current => new Tile(_x, _y, _tileRange.Zoom);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                
            }
        }
    }
}