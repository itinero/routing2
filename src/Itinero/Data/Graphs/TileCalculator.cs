using System;

namespace Itinero.Data.Graphs
{
    public class TileCalculator
    {
        private class Tile
        {
            public uint X { get; set; }

            public uint Y { get; set; }

            public uint Zoom { get; set; }

            public ulong LocalId
            {
                get
                {
                    ulong xMax = (ulong) (1 << (int) this.Zoom);

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

            public static Tile FromLocalId(uint zoom, uint localId)
            {
                ulong xMax = (ulong) (1 << (int) zoom);

                return new Tile()
                {
                    X = (uint) (localId % xMax),
                    Y = (uint) (localId / xMax),
                    Zoom = zoom
                };
            }
        }

        /// <summary>
        /// Converts lat/lon to tile coordinates.
        /// </summary>
        public static ulong WorldToTileIndex(double latitude, double longitude, uint zoom)
        {
            var n = (int) Math.Floor(Math.Pow(2, zoom));

            var rad = (latitude / 180d) * System.Math.PI;

            var x = (uint) ((longitude + 180.0f) / 360.0f * n);
            var y = (uint) (
                (1.0f - Math.Log(Math.Tan(rad) + 1.0f / Math.Cos(rad))
                 / Math.PI) / 2f * n);

            return new Tile {X = x, Y = y, Zoom = zoom};
        }

        /// <summary>
        /// Converts tile coordinates to lat/lon.
        /// </summary>
        public static (double latitude, double longitude) TileIndexToWorld(double tileX, double tileY, uint zoom)
        {
            var n = Math.PI - 2.0 * Math.PI * tileY / Math.Pow(2.0, zoom);
            return (180.0 / Math.PI * Math.Atan(Math.Sinh(n)),
                (tileX / Math.Pow(2.0, zoom) * 360.0 - 180.0));
        }
    }
}