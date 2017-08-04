using Itinero.LocalGeo;
using System;

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