using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.IO.Json.GeoJson;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles.Parsers;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Snapping;
using Itinero.Tests.Functional.Download;
using Itinero.Tests.Functional.Tests;
using OsmSharp.Logging;
using OsmSharp.Streams;
using OsmSharp.Streams.Filters;
using Serilog;
using Serilog.Events;
using TraceEventType = Itinero.Logging.TraceEventType;

namespace Itinero.Tests.Functional
{
    internal static class Program
    {
        private static readonly string LuxembourgUrl =
            "http://planet.anyways.eu/planet/europe/luxembourg/luxembourg-latest.osm.pbf";

        private static readonly string BelgiumUrl =
            "http://planet.anyways.eu/planet/europe/belgium/belgium-latest.osm.pbf";

        private static RouterDb FromFile(string filepath)
        {
            Console.WriteLine("Loading from file " + filepath);
            using var routerDbStream = File.OpenRead(filepath);
            return RouterDb.ReadFrom(routerDbStream);
        }

        private static void ToFile(string path, RouterDb routerDb)
        {
            Console.WriteLine("Writing to file " + path);

            using var outputStream = File.Open(path, FileMode.Create);
            routerDb.WriteTo(outputStream);
        }

        private static RouterDb FromUrl(Profile p, string url, string localFile = "latest.osm.pbf")
        {
            Console.WriteLine("Loading from URL " + url);
            var routerDb = new RouterDb(new RouterDbConfiguration {
                Zoom = 14
            });

            routerDb.PrepareFor(p);
            using var osmStream = File.OpenRead(Staging.Download.Get(localFile, url));
            var progress = new OsmStreamFilterProgress();
            var osmPbfStream = new PBFOsmStreamSource(osmStream);
            progress.RegisterSource(osmPbfStream);

            routerDb.UseOsmData(progress);

            return routerDb;
        }


        private static void Main(string[] args)
        {
            EnableLogging();

            TileParser.DownloadFunc = DownloadHelper.Download;

            // // create a new srtm data instance.
            // // it accepts a folder to download and cache data into.
            // var srtmCache = new DirectoryInfo("srtm-cache");
            // if (!srtmCache.Exists) {
            //     srtmCache.Create();
            // }

            // // setup elevation integration.
            // var srtmData = new SRTMData(srtmCache.FullName) {
            //     GetMissingCell = (path, name) => {
            //         var filename = name + ".hgt.zip";
            //         var hgt = Path.Combine(path, filename);
            //
            //         if (SourceHelpers.Download(hgt, "http://planet.anyways.eu/srtm/" + filename)) {
            //             return true;
            //         }
            //
            //         return false;
            //     }
            // };
            //
            // ElevationHandler.Default = new ElevationHandler((lat, lon) => {
            //     var elevation = srtmData.GetElevation(lat, lon);
            //     if (!elevation.HasValue) {
            //         return 0;
            //     }
            //
            //     return (short) elevation;
            // });

            var bicycle = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;

            // setup a router db with a local osm file.
            RouterDb routerDb = FromUrl(bicycle, LuxembourgUrl, "luxembourg-latest.osm.pbf");

            var lux1 = (6.119298934936523, 49.60962540702068, (float?) 0f);
            var lux2 = (6.124148368835449, 49.588792167215345, (float?) 0f);

            var latest = routerDb.Latest;
            var lux1sp = latest.Snap().To(lux1);
            var lux2sp = latest.Snap().To(lux2);

            var oneToOne = RouterOneToOneTest.Default.Run((latest, lux1sp, lux2sp, bicycle));
            var oneToOneGeoJson = oneToOne.ToGeoJson();
            var routes = RouterOneToOneWithAlternativeTest.Default.Run(
                (latest, lux1sp, lux2sp, bicycle)
            );
            
            var geoJson = routes.Select(r => r.ToGeoJson()).ToList();
            Console.WriteLine(geoJson);
            // SnappingTests.RunTests(routerDb, bicycle);
            //  routerDb = FromFile("/data/work/data/OSM/test/itinero2/data.routerdb");
            // SnappingTests.RunTestsBe(routerDb, bicycle);

            //
            // var route = RouterOneToOneTest.Default.Run((latest, lesotho1, lesotho2, bicycle),
            //     $"Route cold: {nameof(lesotho1)} -> {nameof(lesotho2)}");
            // route = RouterOneToOneTest.Default.Run((latest, lesotho1, lesotho2, bicycle),
            //     $"Route hot: {nameof(lesotho1)} -> {nameof(lesotho2)}", 100);
            // File.WriteAllText(Path.Combine("results", $"{nameof(lesotho1)}-{nameof(lesotho2)}.geojson"), 
            //     route.ToGeoJson());
            //
            // routerDb = RouterDbSerializeDeserializeTest.Default.Run(routerDb,
            //     "Serializing/deserializing current routerdb.");
            // latest = routerDb.Network;
            //
            // route = RouterOneToOneTest.Default.Run((latest, lesotho1, lesotho2, bicycle),
            //     $"Route cold (after deserialization): {nameof(lesotho1)} -> {nameof(lesotho2)}");
            // route = RouterOneToOneTest.Default.Run((latest, lesotho1, lesotho2, bicycle),
            //     $"Route hot (after deserialization): {nameof(lesotho1)} -> {nameof(lesotho2)}", 100);
            // File.WriteAllText(Path.Combine("results", $"{nameof(lesotho1)}-{nameof(lesotho2)}-after.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
            //     $"Route cold: {nameof(zellik1)} -> {nameof(zellik2)}");
            // route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
            //     $"Route hot: {nameof(zellik1)} -> {nameof(zellik2)}", 100);
            // File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, wechelderzande1, vorselaar1, bicycle),
            //     $"Route cold: {nameof(wechelderzande1)} -> {nameof(vorselaar1)}");
            // route = RouterOneToOneTest.Default.Run((latest, wechelderzande1, vorselaar1, bicycle),
            //     $"Route hot: {nameof(wechelderzande1)} -> {nameof(vorselaar1)}", 100);
            // File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande1)}-{nameof(vorselaar1)}.geojson"), 
            //     route.ToGeoJson());
            //
            // routerDb = RouterDbSerializeDeserializeTest.Default.Run(routerDb,
            //     "Serializing/deserializing current routerdb.");
            // latest = routerDb.Network;
            //
            // route = RouterOneToOneTest.Default.Run((latest, wechelderzande1, vorselaar1, bicycle),
            //     $"Route cold (after deserialization): {nameof(wechelderzande1)} -> {nameof(vorselaar1)}");
            // route = RouterOneToOneTest.Default.Run((latest, wechelderzande1, vorselaar1, bicycle),
            //     $"Route hot (after deserialization): {nameof(wechelderzande1)} -> {nameof(vorselaar1)}", 100);
            // File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande1)}-{nameof(vorselaar1)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
            //     $"Route cold: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}"); 
            // route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
            //     $"Route host: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}");
            // File.WriteAllText(Path.Combine("results", $"{nameof(bruggeStation)}-{nameof(stationDuinberge)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
            //     $"Route cold: {nameof(zellik1)} -> {nameof(zellik2)}");
            // route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
            //     $"Route hot: {nameof(zellik1)} -> {nameof(zellik2)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
            //     $"Route cold: {nameof(zellik2)} -> {nameof(zellik1)}");
            // route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
            //     $"Route hot: {nameof(zellik2)} -> {nameof(zellik1)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(zellik2)}-{nameof(zellik1)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
            //     $"Route cold: {nameof(heldergem)} -> {nameof(ninove)}");
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
            //     $"Route hot: {nameof(heldergem)} -> {nameof(ninove)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}.geojson"), 
            //     route.ToGeoJson());
            //
            // Parallel.For(0, 10, (i) =>
            // {
            //     RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
            //         $"Routing parallel: {nameof(heldergem)} -> {nameof(ninove)}");
            // });
            //
            // route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
            //     $"Route (after deserialization) cold: {nameof(zellik1)} -> {nameof(zellik2)}");
            // route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
            //     $"Route (after deserialization) hot: {nameof(zellik1)} -> {nameof(zellik2)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}-deserialized.geojson"), 
            //     route.ToGeoJson());
            //     
            // route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
            //     $"Route (after deserialization) cold: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}"); 
            // route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
            //     $"Route (after deserialization) host: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}");
            // File.WriteAllText(Path.Combine("results", $"{nameof(bruggeStation)}-{nameof(stationDuinberge)}-deserialized.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
            //     $"Route (after deserialization) cold: {nameof(zellik2)} -> {nameof(zellik1)}");
            // route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
            //     $"Route (after deserialization) hot: {nameof(zellik2)} -> {nameof(zellik1)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(zellik2)}-{nameof(zellik1)}-deserialized.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
            //     $"Route (after deserialization) cold: {nameof(heldergem)} -> {nameof(ninove)}");
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
            //     $"Route (after deserialization) hot: {nameof(heldergem)} -> {nameof(ninove)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}-deserialized.geojson"), 
            //     route.ToGeoJson());
            //
            // Parallel.For(0, 10, (i) =>
            // {
            //     RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
            //         $"Routing (after deserialization) parallel: {nameof(heldergem)} -> {nameof(ninove)}");
            // });
            //
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, pepingen, bicycle),
            //     $"Route cold: {nameof(heldergem)} -> {nameof(pepingen)}");
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, pepingen, bicycle),
            //     $"Route hot: {nameof(heldergem)} -> {nameof(pepingen)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(pepingen)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, lebbeke, bicycle),
            //     $"Route cold: {nameof(heldergem)} -> {nameof(lebbeke)}");
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, lebbeke, bicycle),
            //     $"Route hot: {nameof(heldergem)} -> {nameof(lebbeke)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(lebbeke)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, stekene, bicycle),
            //     $"Route cold: {nameof(heldergem)} -> {nameof(stekene)}");
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, stekene, bicycle),
            //     $"Route hot: {nameof(heldergem)} -> {nameof(stekene)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(stekene)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, hamme, bicycle),
            //     $"Route cold: {nameof(heldergem)} -> {nameof(hamme)}");
            // route = RouterOneToOneTest.Default.Run((latest, heldergem, hamme, bicycle),
            //     $"Route hot: {nameof(heldergem)} -> {nameof(hamme)}", 10);
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(hamme)}.geojson"), 
            //     route.ToGeoJson());
            //
            // route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande5, DirectionEnum.East),
            //     (wechelderzande2, DirectionEnum.West), bicycle));
            // File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.East)}-" +
            //                                           $"{nameof(wechelderzande2)}_{nameof(DirectionEnum.West)}.geojson"),route.ToGeoJson());
            // route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande5, DirectionEnum.East),
            //     (wechelderzande2, null), bicycle));
            // File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.East)}-" +
            //                                           $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());
            // route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande5, DirectionEnum.West),
            //     (wechelderzande2, null), bicycle));
            // File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.West)}-" +
            //                                           $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());
            //
            // route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande4, DirectionEnum.South),
            //     (wechelderzande2, null), bicycle));
            // File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande4)}_{nameof(DirectionEnum.South)}-" +
            //                                           $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());
            // route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande4, DirectionEnum.North),
            //     (wechelderzande2, null), bicycle));
            // File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande4)}_{nameof(DirectionEnum.North)}-" +
            //                                           $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());
            //
            // var oneToManyRoutes = RouterOneToManyTest.Default.Run(
            //     (latest, heldergem, new[] {ninove, pepingen, lebbeke}, bicycle),
            //     $"Routes (one to many) cold: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            // oneToManyRoutes = RouterOneToManyTest.Default.Run(
            //     (latest, heldergem, new[] {ninove, pepingen, lebbeke}, bicycle),
            //     $"Routes (one to many) hot: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-0.geojson"),
            //     oneToManyRoutes[0].ToGeoJson());
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
            //     oneToManyRoutes[1].ToGeoJson());
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-2.geojson"),
            //     oneToManyRoutes[2].ToGeoJson());
            //
            // // tests for many to one routing.
            // var manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, new [] {ninove, pepingen, lebbeke}, heldergem, bicycle),
            //     $"Routes (many to one) cold: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
            // manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, new [] {ninove, pepingen, lebbeke}, heldergem, bicycle),
            //     $"Routes (many to one) hot: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
            // File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-0.geojson"),
            //     manyToOneRoutes[0].ToGeoJson());
            // File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-1.geojson"),
            //     manyToOneRoutes[1].ToGeoJson());
            // File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-2.geojson"),
            //     manyToOneRoutes[2].ToGeoJson());
            //
            // manyToOneRoutes = RouterOneToManyTest.Default.Run((latest, heldergem, new [] {ninove, pepingen, lebbeke}, bicycle),
            //     $"Routes (one to many) cold: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            // manyToOneRoutes = RouterOneToManyTest.Default.Run((latest, heldergem, new [] {ninove, pepingen, lebbeke}, bicycle),
            //     $"Routes (one to many) hot: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-0.geojson"),
            //     manyToOneRoutes[0].ToGeoJson());
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
            //     manyToOneRoutes[1].ToGeoJson());
            // File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
            //     manyToOneRoutes[2].ToGeoJson());
            //
            // for (var j = 0; j < 5; j++)
            // {
            //     // setup a router db with a routable tiles data provider.
            //     routerDb = new RouterDb(new RouterDbConfiguration()
            //     {
            //         Zoom = 14
            //     });
            //     routerDb.Mutate(mutable =>
            //     {
            //         mutable.PrepareFor(bicycle);
            //     });
            //     routerDb.UseRouteableTiles(s =>
            //     {
            //         s.Url = "https://data1.anyways.eu/tiles/20200527-080000";
            //     });
            //     latest = routerDb.Network;
            //
            //     var deSterre = SnappingTest.Default.Run((latest, 3.715675, 51.026164, profile: bicycle),
            //         $"Snapping cold: deSterre");
            //
            //     var targets = new[]
            //     {
            //         SnappingTest.Default.Run((latest, 3.70137870311737, 51.1075870861261, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.7190705537796, 51.0883577942415, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.74058723449707, 51.0563671057799, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.65520179271698, 51.0472956366036, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.71066987514496, 51.0358985842182, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.75632107257843, 51.0386479278862, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.76888453960419, 51.0175340229811, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.79708528518677, 51.0028081059898, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.65046501159668, 50.9970656297241, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.82958829402924, 50.9917511038545, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.80167186260223, 50.9801985244791, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.62783789634705, 50.960774752016, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.79114151000977, 50.9705486324505, profile: bicycle),
            //             $"Snapping cold."),
            //         SnappingTest.Default.Run((latest, 3.78133535385132, 50.9535197402615, profile: bicycle),
            //             $"Snapping cold."),
            //     };
            //     
            //     Parallel.For(0, 10, (i) =>
            //     {
            //         manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, targets, deSterre, bicycle),
            //             $"Route cold: many to one to {nameof(deSterre)}");
            //         manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, targets, deSterre, bicycle),
            //             $"Route hot: many to one to {nameof(deSterre)}");
            //     });
            // }
        }

        private static void EnableLogging()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
#if DEBUG
            var loggingBlacklist = new HashSet<string>();
#else
            var loggingBlacklist = new HashSet<string>();
#endif
            Logger.LogAction = (o, level, message, parameters) => {
                if (loggingBlacklist.Contains(o)) {
                    return;
                }

                if (!string.IsNullOrEmpty(o)) {
                    message = $"[{o}] {message}";
                }

                if (level == TraceEventType.Verbose.ToString().ToLower()) {
                    Log.Debug(message);
                }
                else if (level == TraceEventType.Information.ToString().ToLower()) {
                    Log.Information(message);
                }
                else if (level == TraceEventType.Warning.ToString().ToLower()) {
                    Log.Warning(message);
                }
                else if (level == TraceEventType.Critical.ToString().ToLower()) {
                    Log.Fatal(message);
                }
                else if (level == TraceEventType.Error.ToString().ToLower()) {
                    Log.Error(message);
                }
                else {
                    Log.Debug(message);
                }
            };

            Logging.Logger.LogAction = (o, level, message, parameters) => {
                if (loggingBlacklist.Contains(o)) {
                    return;
                }

                if (!string.IsNullOrEmpty(o)) {
                    message = $"[{o}] {message}";
                }

                if (level == TraceEventType.Verbose.ToString().ToLower()) {
                    Log.Debug(message);
                }
                else if (level == TraceEventType.Information.ToString().ToLower()) {
                    Log.Information(message);
                }
                else if (level == TraceEventType.Warning.ToString().ToLower()) {
                    Log.Warning(message);
                }
                else if (level == TraceEventType.Critical.ToString().ToLower()) {
                    Log.Fatal(message);
                }
                else if (level == TraceEventType.Error.ToString().ToLower()) {
                    Log.Error(message);
                }
                else {
                    Log.Debug(message);
                }
            };
        }
    }
}