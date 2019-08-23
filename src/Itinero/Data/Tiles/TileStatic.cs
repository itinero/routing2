using System;

namespace Itinero.Data.Tiles
{
    internal static class TileStatic
    {
        public static (uint x, uint y) ToTile(int zoom, uint tileId)
        {
            var xMax = (ulong) (1 << zoom);

            return ((uint) (tileId % xMax), (uint) (tileId / xMax));
        }

        public static uint ToLocalId(uint x, uint y, int zoom)
        {
            var xMax = (1 << (int) zoom);
            return (uint)(x * xMax + y);
        }
        
        public static (int x, int y) ToLocalTileCoordinates(int zoom, uint tileId, double longitude, double latitude, int resolution)
        {
            var tile = ToTile(zoom, tileId);
            
            var n = Math.PI - ((2.0 * Math.PI * tile.y) / Math.Pow(2.0, zoom));
            var left = ((tile.x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            var top = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

            n = Math.PI - ((2.0 * Math.PI * (tile.y + 1)) / Math.Pow(2.0, zoom));
            var right = ((tile.x + 1) / Math.Pow(2.0, zoom) * 360.0) - 180.0;
            var bottom = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
            
            var latStep = (top - bottom) / resolution;
            var lonStep = (right - left) / resolution;
            
            return ((int) ((longitude - left) / lonStep), (int) ((top - latitude) / latStep));
        }
        
        public static (uint x, uint y) WorldToTile(double longitude, double latitude, int zoom)
        {
            var n = (int) Math.Floor(Math.Pow(2, zoom)); // replace by bitshifting?

            var rad = (latitude / 180d) * System.Math.PI;

            var x = (uint) ((longitude + 180.0f) / 360.0f * n);
            var y = (uint) (
                (1.0f - Math.Log(Math.Tan(rad) + 1.0f / Math.Cos(rad))
                 / Math.PI) / 2f * n);
            
            return (x, y);
        }
    }
}