using Itinero.LocalGeo;
using System;
using System.Collections.Generic;
using System.IO;

namespace Itinero.Tiled.Tiles
{
    public class Tile
    {
        public uint X { get; set; }

        public uint Y { get; set; }

        public uint Zoom { get; set; }

        public ulong LocalId
        {
            get
            {
                ulong xMax = (ulong)(1 << (int)this.Zoom);

                return this.X + this.Y * xMax;
            }
        }

        public Tile[] Subtiles
        {
            get
            {
                var x = this.X * 2;
                var y = this.Y * 2;
                return new Tile[]
                {
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x,
                        Y = y
                    },
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x + 1,
                        Y = y
                    },
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x,
                        Y = y + 1
                    },
                    new Tile()
                    {
                        Zoom = this.Zoom + 1,
                        X = x + 1,
                        Y = y + 1
                    }
                };
            }
        }
        
        public List<Tile> GetSubtilesAt(uint zoom)
        {
            var tiles = new List<Tile>();

            if (this.Zoom + 1 == zoom)
            {
                tiles.AddRange(this.Subtiles);
            }
            else
            {
                foreach(var tile in this.Subtiles)
                {
                    tiles.AddRange(tile.GetSubtilesAt(zoom));
                }
            }

            return tiles;
        }

        public static Tile FromLocalId(uint zoom, uint localId)
        {
            ulong xMax = (ulong)(1 << (int)zoom);

            return new Tile()
            {
                X = (uint)(localId % xMax),
                Y = (uint)(localId / xMax),
                Zoom = zoom
            };
        }

        public void ToPoly(TextWriter stream)
        {
            stream.WriteLine(string.Format("{0}_{1}_{2}",
                this.Zoom, this.X, this.Y));
            stream.WriteLine("1");
            
            var c = Tiles.Tile.TileIndexToWorld(this.X, this.Y, this.Zoom);
            stream.Write(c.Longitude.ToInvariantString());
            stream.Write(' ');
            stream.WriteLine(c.Latitude.ToInvariantString());
            c = Tiles.Tile.TileIndexToWorld(this.X + 1, this.Y, this.Zoom);
            stream.Write(c.Longitude.ToInvariantString());
            stream.Write(' ');
            stream.WriteLine(c.Latitude.ToInvariantString());
            c = Tiles.Tile.TileIndexToWorld(this.X + 1, this.Y + 1, this.Zoom);
            stream.Write(c.Longitude.ToInvariantString());
            stream.Write(' ');
            stream.WriteLine(c.Latitude.ToInvariantString());
            c = Tiles.Tile.TileIndexToWorld(this.X, this.Y + 1, this.Zoom);
            stream.Write(c.Longitude.ToInvariantString());
            stream.Write(' ');
            stream.WriteLine(c.Latitude.ToInvariantString());
            stream.WriteLine("END");
            stream.WriteLine("END");
        }

        /// <summary>
        /// Converts lat/lon to tile coordinates.
        /// </summary>
        public static Tile WorldToTileIndex(double latitude, double longitude, uint zoom)
        {
            var n = (int)Math.Floor(Math.Pow(2, zoom));

            var rad = (latitude / 180d) * System.Math.PI;

            var x = (uint)((longitude + 180.0f) / 360.0f * n);
            var y = (uint)(
                (1.0f - Math.Log(Math.Tan(rad) + 1.0f / Math.Cos(rad))
                / Math.PI) / 2f * n);

            return new Tile { X = x, Y = y, Zoom = zoom };
        }

        /// <summary>
        /// Converts tile coordinates to lat/lon.
        /// </summary>
        public static Coordinate TileIndexToWorld(double tileX, double tileY, uint zoom)
        {
            var n = Math.PI - 2.0 * Math.PI * tileY / Math.Pow(2.0, zoom);
            return new Coordinate(
                (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n))),
                (float)(tileX / Math.Pow(2.0, zoom) * 360.0 - 180.0));
        }
    }
}