using System;
using System.IO;

namespace Itinero.Tiled.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };
            Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };

            var source = new OsmSharp.Streams.PBFOsmStreamSource(File.OpenRead(@"C:\work\data\OSM\belgium-latest.osm.pbf"));

            var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
            progress.RegisterSource(source);

            var db = new RouterDbTiled(11);
            var target = new IO.RouterDbTiledStreamTarget(db, new Profiles.Vehicle[] { Itinero.Osm.Vehicles.Vehicle.Car });
            target.RegisterSource(progress);
            target.Pull();

            var count = 0;
            foreach (var tileId in db.Graph.TileIds)
            {
                count++;
                var tile = db.Graph.GetTile(tileId);

                var json = db.GetGeoJson(tileId, true, false, true);
                File.WriteAllText(string.Format("{0}.geojson", tileId), json);

                Console.WriteLine("Tile found: {0}", tileId);
            }

            Console.WriteLine("{0} tiles found!", count);
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}