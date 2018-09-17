using System;
using System.IO;
using Itinero.Data.Tiles;
using Itinero.LocalGeo;
using OsmSharp;

namespace Itinero.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            OsmSharp.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}-{3}] {1} - {2}", origin, level, message, DateTime.Now.ToString()));
            };
            
            int resolution = 4096;
            uint zoom = 14;

            var maxDistance = 0.0;
            using (var stream = File.OpenRead(@"/home/xivk/work/data/OSM/belgium-latest.osm.pbf"))
            {
                var source = new OsmSharp.Streams.PBFOsmStreamSource(stream);
                var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
                progress.RegisterSource(source);
                foreach (var osmGeo in progress)
                {
                    if (!(osmGeo is Node node)) break;

                    var tile = Tile.WorldToTile(node.Latitude.Value, node.Longitude.Value, zoom);
                    var localCoordinates = tile.ToLocalCoordinates(node.Latitude.Value, node.Longitude.Value, resolution);
                    var globalCoordinates = tile.FromLocalCoordinates(localCoordinates.x, localCoordinates.y, resolution);

                    var distance = Coordinate.DistanceEstimateInMeter(node.Latitude.Value, node.Longitude.Value,
                        globalCoordinates.latitude, globalCoordinates.longitude);

                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        Console.WriteLine($"Max distance: {maxDistance}");
                        File.AppendAllLines($"max-distance-{zoom}-{resolution}.csv", new string[] { $"{node.Id.Value};{node.Latitude.Value};" +
                                                                                     $"{node.Longitude.Value},{distance}" });
                    }
                }
            }
        }
    }
}
