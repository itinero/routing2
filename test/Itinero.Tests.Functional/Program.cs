using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Algorithms.Dijkstra;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles;
using Itinero.IO.Osm.Tiles.Parsers;
using Itinero.IO.Shape;
using Itinero.LocalGeo;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Tests.Functional.Staging;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using OsmSharp;

namespace Itinero.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            EnableLogging();
            
            // do some local caching.
            TileParser.DownloadFunc = Download.DownloadHelper.Download;
            
            // setup a router db with a routable tiles data provider..
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var bicycle = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
            var pedestrian = Itinero.Profiles.Lua.Osm.OsmProfiles.Pedestrian;

            var profile = LuaProfile.Load(File.ReadAllText(@"bicycle.lua"));

            var sp1 = routerDb.Snap(3.218109999999996,51.21459999999999);
            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
            var sp2 = routerDb.Snap(3.2167249917984009, 51.197229555160746);
            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
            var sp1Geojson = routerDb.ToFeatureCollection(sp1).ToGeoJson();
            var sp2Geojson = routerDb.ToFeatureCollection(sp2).ToGeoJson();
            var route = routerDb.Calculate(bicycle,sp1, sp2);
            File.WriteAllText("route1-short.geojson", routerDb.ToGeoJson(route));

            sp1 = routerDb.Snap(4.308834671974182, 50.869586751922704);
            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
            sp2 = routerDb.Snap(4.30814266204834, 50.869309146821486);
            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
            sp1Geojson = routerDb.ToFeatureCollection(sp1).ToGeoJson();
            sp2Geojson = routerDb.ToFeatureCollection(sp2).ToGeoJson();
            route = routerDb.Calculate(bicycle,sp1, sp2);
            File.WriteAllText("route1-short.geojson", routerDb.ToGeoJson(route));
            
            Console.WriteLine("Calculating route1");
            sp1 = routerDb.Snap(4.309666156768798, 50.87108985327193);
            sp2 = routerDb.Snap(4.270634651184082, 50.86964430399289);
            sp1Geojson = routerDb.ToFeatureCollection(sp1).ToGeoJson();
            sp2Geojson = routerDb.ToFeatureCollection(sp2).ToGeoJson();
            route = routerDb.Calculate(bicycle,sp1, sp2);
            File.WriteAllText("route1.geojson",routerDb.ToGeoJson(route));
            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
            
            Console.WriteLine("Calculating route1 - pedestrian");
            sp1 = routerDb.Snap(4.309666156768798, 50.87108985327193);
            sp2 = routerDb.Snap(4.270634651184082, 50.86964430399289);
            sp1Geojson = routerDb.ToFeatureCollection(sp1).ToGeoJson();
            sp2Geojson = routerDb.ToFeatureCollection(sp2).ToGeoJson();
            route = routerDb.Calculate(pedestrian,sp1, sp2);
            File.WriteAllText("route1-pedestrian.geojson",routerDb.ToGeoJson(route));

            Console.WriteLine("Calculating route2");
            sp1 = routerDb.Snap(4.801840782165527, 51.267903074610615);
            sp2 = routerDb.Snap(4.7806620597839355, 51.2609614991932);
            route = routerDb.Calculate(bicycle, sp1, sp2);
            File.WriteAllText("route2.geojson",routerDb.ToGeoJson(route));

            Console.WriteLine("Calculating route3");
            sp1 = routerDb.Snap(-68.8235092163086, -32.844836958416735);
            sp2 = routerDb.Snap(-68.84187698364256, -32.88167751934565);
            route = routerDb.Calculate(bicycle, sp1, sp2);
            File.WriteAllText("route3.geojson",routerDb.ToGeoJson(route));

            Console.WriteLine("Calculating route4");
            sp1 = routerDb.Snap( 4.801915884017944,51.26795342069926);
            sp2 = routerDb.Snap(4.780729115009308, 51.26100681751947);
            route = routerDb.Calculate(bicycle, sp1, sp2);
            File.WriteAllText("route4.geojson",routerDb.ToGeoJson(route));

            sp1 = routerDb.Snap(149.19013023376465, -21.12181472572919);
            sp2 = routerDb.Snap(148.94960045814514, -21.14731300412497);
            route = routerDb.Calculate(bicycle, sp1, sp2);
            var json = (routerDb.ToFeatureCollection(route)).ToGeoJson();
            File.WriteAllText("route5.geojson",json);
            
            // (51.2146, 3.21811) to (51.22312,3.20179)
            
            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
            
//            var box = (4.7806620597839355 - 0.001, 51.2609614991932 - 0.001,
//                4.7806620597839355 + 0.001, 51.2609614991932 + 0.001);
//            routerDb.DataProvider?.TouchBox(box);
//            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
//            
//            var features = new FeatureCollection();
//            features.AddRange(routerDb.ToFeaturesVertices(box));
//            var json = features.ToGeoJson();
//            featureCollection = routerDb.ToFeatureCollection(path);
//            json = (new GeoJsonWriter()).Write(featureCollection);
//            
//            routerDb.WriteToShape("test");

//            var kempen = (4.5366668701171875, 51.179773424875634,
//                4.8017120361328125, 51.29885215199866);
//            var brussel = (4.1143798828125, 50.69471783819287, 
//                4.5977783203125, 50.975723786793324);
//            var brusselBug = (4.354190826416016, 50.84426710838739, 
//                4.371228218078613, 50.85109531786361);
//            var routerDb = new RouterDb();
//            routerDb.LoadOsmDataFromTiles(brusselBug);
//            routerDb.WriteToShape("test");
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
