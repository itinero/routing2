using System;
using System.Collections.Generic;

namespace Itinero.Network.Tiles
{
    internal static class TileStatic
    {
        private static void ValidateBox(this ((double longitude, double latitude, float? e) topLeft,
            (double longitude, double latitude, float? e) bottomRight) box)
        {
            if (box.topLeft.latitude < box.bottomRight.latitude)
            {
                throw new ArgumentOutOfRangeException($"Top is lower than bottom.");
            }
        }

        public static (uint x, uint y) ToTile(int zoom, uint tileId)
        {
            var xMax = (ulong)(1 << zoom);

            return ((uint)(tileId % xMax), (uint)(tileId / xMax));
        }

        public static uint ToLocalId(uint x, uint y, int zoom)
        {
            var xMax = 1 << (int)zoom;
            return (uint)(y * xMax + x);
        }

        public static uint ToLocalId(double longitude, double latitude, int zoom)
        {
            var (x, y) = WorldToTile(longitude, latitude, zoom);
            return ToLocalId(x, y, zoom);
        }

        public static (int x, int y) ToLocalTileCoordinates(int zoom, uint tileId, double longitude, double latitude,
            int resolution)
        {
            var tile = ToTile(zoom, tileId);

            var n = Math.PI - 2.0 * Math.PI * tile.y / Math.Pow(2.0, zoom);
            var left = tile.x / Math.Pow(2.0, zoom) * 360.0 - 180.0;
            var top = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));

            n = Math.PI - 2.0 * Math.PI * (tile.y + 1) / Math.Pow(2.0, zoom);
            var right = (tile.x + 1) / Math.Pow(2.0, zoom) * 360.0 - 180.0;
            var bottom = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));

            var latStep = (top - bottom) / resolution;
            var lonStep = (right - left) / resolution;

            return ((int)((longitude - left) / lonStep), (int)((top - latitude) / latStep));
        }

        public static void FromLocalTileCoordinates(int zoom, uint tileId, int x, int y, int resolution,
            out double longitude, out double latitude)
        {
            var tile = ToTile(zoom, tileId);

            var n = Math.PI - 2.0 * Math.PI * tile.y / Math.Pow(2.0, zoom);
            var left = tile.x / Math.Pow(2.0, zoom) * 360.0 - 180.0;
            var top = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));

            n = Math.PI - 2.0 * Math.PI * (tile.y + 1) / Math.Pow(2.0, zoom);
            var right = (tile.x + 1) / Math.Pow(2.0, zoom) * 360.0 - 180.0;
            var bottom = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));

            var latStep = (top - bottom) / resolution;
            var lonStep = (right - left) / resolution;

            longitude = left + lonStep * x;
            latitude = top - y * latStep;
        }

        private static int N(int zoom)
        {
            return (int)Math.Floor(Math.Pow(2, zoom)); // replace by bit shifting?
        }

        public static (uint x, uint y) WorldToTile(double longitude, double latitude, int zoom)
        {
            var n = N(zoom);
            var rad = latitude / 180d * Math.PI;

            var x = (uint)((longitude + 180.0f) / 360.0f * n);
            var y = (uint)(
                (1.0f - Math.Log(Math.Tan(rad) + 1.0f / Math.Cos(rad))
                    / Math.PI) / 2f * n);

            return (x, y);
        }

        public static IEnumerable<(uint x, uint y)> TileRange(
            this ((double longitude, double latitude, float? e) topLeft,
                (double longitude, double latitude, float? e) bottomRight) box, int zoom)
        {
            box.ValidateBox();

            var n = N(zoom);
            var topLeft = WorldToTile(box.topLeft.longitude, box.topLeft.latitude, zoom);
            var bottomRight = WorldToTile(box.bottomRight.longitude, box.bottomRight.latitude, zoom);

            var x = topLeft.x;
            var y = topLeft.y;
            while (true)
            {
                yield return (x, y);

                if (y == bottomRight.y)
                {
                    // move on with x.
                    if (x == bottomRight.x)
                    {
                        // both x and y have reached the end.
                        break;
                    }

                    // reset y.
                    y = topLeft.y;

                    // move on with x.
                    x++;
                    if (x == n)
                    {
                        x = 0;
                    }

                    continue;
                }

                // move on with y.
                y++;
            }
        }
    }
}
