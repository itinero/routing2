using System;
using System.Collections.Generic;
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

            var resolutions = new int[] { 1024, 2048, 4096, 8192, 16384, 32768 };
            var zooms = new uint[] {10, 11, 12, 13, 14 };
            var worst = new Dictionary<(int resolution, uint zoom), double>();

            using (var stream = File.OpenRead(@"/home/xivk/work/data/OSM/belgium-latest.osm.pbf"))
            {
                var source = new OsmSharp.Streams.PBFOsmStreamSource(stream);
                var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
                progress.RegisterSource(source);
                foreach (var osmGeo in progress)
                {
                    if (!(osmGeo is Node node)) break;

                    foreach (var zoom in zooms)
                    {
                        var tile = Tile.WorldToTile(node.Latitude.Value, node.Longitude.Value, zoom);
                        foreach (var resolution in resolutions)
                        {
                            var current = (resolution, zoom);
                            if (!worst.TryGetValue(current, out var currentWorst))
                            {
                                currentWorst = 0.0;
                            }

                            var localCoordinates =
                                tile.ToLocalCoordinates(node.Latitude.Value, node.Longitude.Value, resolution);
                            var globalCoordinates =
                                tile.FromLocalCoordinates(localCoordinates.x, localCoordinates.y, resolution);

                            var distance = Coordinate.DistanceEstimateInMeter(node.Latitude.Value, node.Longitude.Value,
                                globalCoordinates.latitude, globalCoordinates.longitude);

                            if (distance > currentWorst)
                            {
                                worst[current] = distance;
                                Console.WriteLine($"New worst: {current}: {distance}");
                            }
                        }
                    }
                }
            }

            foreach (var w in worst)
            {
                File.AppendAllLines($"worst.csv", new string[] { $"{w.Key.zoom};{w.Key.resolution};{w.Value}" });
            }
        }
    }
}
