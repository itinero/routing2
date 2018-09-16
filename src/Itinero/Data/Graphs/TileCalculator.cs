//using System;
//
//namespace Itinero.Data.Graphs
//{
//    public class TileCalculator
//    {
//        private class Tile
//        {
//            public uint X { get; set; }
//
//            public uint Y { get; set; }
//
//            public uint Zoom { get; set; }
//            
//            public void CalculateBounds()
//            {
//                var n = Math.PI - ((2.0 * Math.PI * this.Y) / Math.Pow(2.0, this.Zoom));
//                this.Left = (float)((this.X / Math.Pow(2.0, this.Zoom) * 360.0) - 180.0);
//                this.Top = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
//
//                n = Math.PI - ((2.0 * Math.PI * (this.Y + 1)) / Math.Pow(2.0, this.Zoom));
//                this.Right = (float)(((this.X + 1) / Math.Pow(2.0, this.Zoom) * 360.0) - 180.0);
//                this.Bottom = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
//
//                this.CenterLat = (float)((this.Top + this.Bottom) / 2.0);
//                this.CenterLon = (float)((this.Left + this.Right) / 2.0);
//            }
//            
//            /// <summary>
//            /// Gets the top.
//            /// </summary>
//            public float Top { get; private set; }
//
//            /// <summary>
//            /// Get the bottom.
//            /// </summary>
//            public float Bottom { get; private set; }
//
//            /// <summary>
//            /// Get the left.
//            /// </summary>
//            public float Left { get; private set; }
//
//            /// <summary>
//            /// Gets the right.
//            /// </summary>
//            public float Right { get; private set; }
//
//            /// <summary>
//            /// Gets the center lat.
//            /// </summary>
//            public float CenterLat { get; private set; }
//
//            /// <summary>
//            /// Gets the center lon.
//            /// </summary>
//            public float CenterLon { get; private set; }
//
//            public ulong LocalId
//            {
//                get
//                {
//                    ulong xMax = (ulong) (1 << (int) this.Zoom);
//
//                    return this.X + this.Y * xMax;
//                }
//            }
//
//            public Tile[] Subtiles
//            {
//                get
//                {
//                    var x = this.X * 2;
//                    var y = this.Y * 2;
//                    return new Tile[]
//                    {
//                        new Tile()
//                        {
//                            Zoom = this.Zoom + 1,
//                            X = x,
//                            Y = y
//                        },
//                        new Tile()
//                        {
//                            Zoom = this.Zoom + 1,
//                            X = x + 1,
//                            Y = y
//                        },
//                        new Tile()
//                        {
//                            Zoom = this.Zoom + 1,
//                            X = x,
//                            Y = y + 1
//                        },
//                        new Tile()
//                        {
//                            Zoom = this.Zoom + 1,
//                            X = x + 1,
//                            Y = y + 1
//                        }
//                    };
//                }
//            }
//
//            public static Tile FromLocalId(uint zoom, uint localId)
//            {
//                ulong xMax = (ulong) (1 << (int) zoom);
//
//                return new Tile()
//                {
//                    X = (uint) (localId % xMax),
//                    Y = (uint) (localId / xMax),
//                    Zoom = zoom
//                };
//            }
//        }
//
//        /// <summary>
//        /// Converts tile coordinates to lat/lon.
//        /// </summary>
//        public static (double latitude, double longitude) TileIndexToWorld(double tileX, double tileY, uint zoom)
//        {
//            var n = Math.PI - 2.0 * Math.PI * tileY / Math.Pow(2.0, zoom);
//            return (180.0 / Math.PI * Math.Atan(Math.Sinh(n)),
//                (tileX / Math.Pow(2.0, zoom) * 360.0 - 180.0));
//        }
//    }
//}