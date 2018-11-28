using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles;
using Itinero.IO.Shape;
using Itinero.LocalGeo;
using OsmSharp;

namespace Itinero.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            EnableLogging();
            
            //var routerDb = IO.Osm.RouterDbStreamTargetTests.LoadFrom(@"/home/xivk/work/data/OSM/brussels.osm.pbf");
            //routerDb.WriteToShape("test");

            var kempen = (4.5366668701171875, 51.179773424875634,
                4.8017120361328125, 51.29885215199866);
            var brussel = (4.1143798828125, 50.69471783819287, 
                4.5977783203125, 50.975723786793324);
            var routerDb = new RouterDb();
            routerDb.LoadOsmDataFromTiles(brussel);
            routerDb.WriteToShape("test");
        }
        
//        static void DetermineWorstOffsetForGraph(string osmPbf)
//        {
//            OsmSharp.Logging.Logger.LogAction = (origin, level, message, parameters) =>
//            {
//                Console.WriteLine("[{0}-{3}] {1} - {2}", origin, level, message, DateTime.Now.ToString());
//            };
//
//            var resolutions = new int[] { 1, 2, 3 }; // resolution in bytes.
//            var zooms = new int[] { 14 };
//            var graphs = new List<Graph>();
//            foreach (var resolution in resolutions)
//            {
//                foreach (var zoom in zooms)
//                {
//                    graphs.Add(new Graph(new GraphSettings()
//                    {
//                        TileResolution = resolution,
//                        Zoom = zoom
//                    }));
//                }
//            }
//            var worst = new Dictionary<Graph, double>();
//
//            using (var stream = File.OpenRead(osmPbf))
//            {
//                var source = new OsmSharp.Streams.PBFOsmStreamSource(stream);
//                var progress = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
//                progress.RegisterSource(source);
//                foreach (var osmGeo in progress)
//                {
//                    if (!(osmGeo is Node node)) break;
//                    if (!node.Latitude.HasValue || !node.Longitude.HasValue) continue;
//
//                    foreach (var graph in graphs)
//                    {
//                        if (!worst.TryGetValue(graph, out var currentWorst))
//                        {
//                            currentWorst = 0.0;
//                        }
//
//                        var vertex = graph.AddVertex(node.Longitude.Value, node.Latitude.Value);
//                        var vertexLocation = graph.GetVertex(vertex);
//                        
//                        var distance = Coordinate.DistanceEstimateInMeter(node.Latitude.Value, node.Longitude.Value,
//                            vertexLocation.Latitude, vertexLocation.Longitude);
//
//                        if (distance > currentWorst)
//                        {
//                            worst[graph] = distance;
//                            Console.WriteLine($"New worst: {graph.Settings}: {distance}");
//                        }
//                    }
//                }
//            }
//
//            foreach (var w in worst)
//            {
//                File.AppendAllLines($"worst-graph.csv", new string[] { $"{w.Key.Settings.Zoom};{w.Key.Settings.TileResolution};{w.Value}" });
//            }
//        }

        static void DetermineWorstOffsetPerTileAndResolution(string osmPbf)
        {
            OsmSharp.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine("[{0}-{3}] {1} - {2}", origin, level, message, DateTime.Now.ToString());
            };

            var resolutions = new int[] { 1024, 2048, 4096, 8192, 16384, 32768 };
            var zooms = new int[] {10, 11, 12, 13, 14 };
            var worst = new Dictionary<(int resolution, int zoom), double>();

            using (var stream = File.OpenRead(osmPbf))
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
                                globalCoordinates.Latitude, globalCoordinates.Longitude);

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
        
        private static void EnableLogging()
        {
//#if DEBUG
            var loggingBlacklist = new HashSet<string>();
//#else
//            var loggingBlacklist = new HashSet<string>(
//                new string[] { 
//                    "StreamProgress",
//                    "RouterDbStreamTarget",
//                    "RouterBaseExtensions",
//                    "HierarchyBuilder",
//                    "RestrictionProcessor",
//                    "NodeIndex",
//                    "RouterDb",
//                    "DuplicateEdgeRemover"
//                });
//#endif
            OsmSharp.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                if (loggingBlacklist.Contains(o))
                {
                    return;
                }
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };
            Itinero.Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                if (loggingBlacklist.Contains(o))
                {
                    return;
                }
                Console.WriteLine(string.Format("[{0}] {1} - {2}", o, level, message));
            };
        }
    }
}
