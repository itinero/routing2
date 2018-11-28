using Itinero.IO.Osm.Tiles.Parsers;

namespace Itinero.IO.Osm.Tiles
{
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Loads all OSM data in the given bounding box by using routable tiles.
        /// </summary>
        /// <param name="db">The router db to fill.</param>
        /// <param name="box">The bounding box to fetch tiles for.</param>
        /// <param name="globalIdMap">The keeps a mapping for all vertices that are not fully inside the loaded area.</param>
        public static void LoadOsmDataFromTiles(this RouterDb db, (double minLon, double minLat, double maxLon, double maxLat) box, 
            GlobalIdMap globalIdMap = null)
        {
            // build the tile range.
            var tileRange = new TileRange(box, db.Zoom);
            
            // build global map if no given.
            if (globalIdMap == null)
            {
                globalIdMap = new GlobalIdMap();
            }
            
            // get all the tiles and build the router db.
            foreach (var tile in tileRange)
            {
                db.AddOsmTile(globalIdMap, tile);
            }
        }
    }
}