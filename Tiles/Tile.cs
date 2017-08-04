using System;

namespace routing2.Tiles
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
                ulong xMax = (ulong)(1 >> (int)this.Zoom);

                return this.X + this.Y * xMax;
            }
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
    }
}