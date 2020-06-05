using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Itinero.Geo.Directions;
using Itinero.IO.Json.GeoJson;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles;
using Itinero.IO.Osm.Tiles.Parsers;
using Itinero.Logging;
using Serilog;
using Serilog.Events;

namespace Itinero.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            EnableLogging();
            
            // make sure the results folder exists.
            if (!Directory.Exists("results"))
            {
                Directory.CreateDirectory("results");
            }
            
            // do some local caching.
            if (!Directory.Exists("cache"))
            {
                Directory.CreateDirectory("cache");
            }
            TileParser.DownloadFunc = Download.DownloadHelper.Download;

            var bicycle = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
            var pedestrian = Itinero.Profiles.Lua.Osm.OsmProfiles.Pedestrian;
            
            // // setup a router db with a local osm file.
            // var routerDb = new RouterDb(new RouterDbConfiguration()
            // {
            //     Zoom = 14
            // });
            // using var osmStream = File.OpenRead(Staging.Download.Get("luxembourg-latest.osm.pbf", 
            //     "http://download.geofabrik.de/europe/luxembourg-latest.osm.pbf"));
            // routerDb.UseOsmData(new OsmSharp.Streams.PBFOsmStreamSource(osmStream));
            //
            // var latest = routerDb.Network;
            // var location1 = SnappingTest.Default.Run((latest, 6.142258644104003, 49.86815622289359,
            //     bicycle));
            // var location2 = SnappingTest.Default.Run((latest, 6.151978969573975, 49.86843283237664,
            //     bicycle));
            // var route = RouterOneToOneTest.Default.Run((latest, location1, location2, bicycle));
            // File.WriteAllText(Path.Combine("results", $"{nameof(location1)}-{nameof(location1)}.geojson"), 
            //     route.ToGeoJson());
            
            // setup a router db with a routable tiles data provider.
            var routerDb = new RouterDb(new RouterDbConfiguration()
            {
                Zoom = 14
            });
            routerDb.Mutate(mutable =>
            {
                mutable.PrepareFor(bicycle);
            });
            routerDb.UseRouteableTiles(s =>
            {
                s.Url = "https://data1.anyways.eu/tiles/20200527-080000";
            });
            var latest = routerDb.Network;

            var factor = bicycle.Factor(new [] {
                ("highway", "pedestrian") });

            factor = bicycle.Factor(new [] {
                ("highway", "pedestrian"),
                ("surface", "cobblestone") });

            var heldergem = SnappingTest.Default.Run((latest, 3.95454, 50.88142, profile: bicycle),
                $"Snapping cold: heldergem");
            heldergem = SnappingTest.Default.Run((latest, 3.95454, 50.88142, profile: bicycle),
                $"Snapping hot: heldergem", 1000);
            var ninove = SnappingTest.Default.Run((latest, 4.02573, 50.83963, profile: bicycle),
                $"Snapping hot: ninove");
            var pepingen = SnappingTest.Default.Run((latest, 4.15887, 50.75932, profile: bicycle),
                $"Snapping hot: pepingen");
            var lebbeke = SnappingTest.Default.Run((latest, 4.12864, 50.99926, profile: bicycle),
                $"Snapping cold: lebbeke");
            var hamme = SnappingTest.Default.Run((latest, 4.13418, 51.09707, profile: bicycle),
                $"Snapping cold: hamme");
            var stekene = SnappingTest.Default.Run((latest, 4.03705, 51.20637, profile: bicycle),
                $"Snapping cold: stekene");
            var leuven = SnappingTest.Default.Run((latest, 4.69575, 50.88040, profile: bicycle),
                $"Snapping cold: leuven");
            var wechelderzande1 = SnappingTest.Default.Run((latest, 4.80129, 51.26774, profile: bicycle),
                $"Snapping cold: wechelderzande1");
            var wechelderzande2 = SnappingTest.Default.Run((latest, 4.794577360153198, 51.26723850107129, profile: bicycle),
                $"Snapping cold: wechelderzande2");
            var wechelderzande3 = SnappingTest.Default.Run((latest, 4.783204793930054, 51.266842437522904, profile: bicycle),
                $"Snapping cold: wechelderzande3");
            var wechelderzande4 = SnappingTest.Default.Run((latest, 4.796256422996521, 51.261015209797186, profile: bicycle),
                $"Snapping cold: wechelderzande4");
            var wechelderzande5 = SnappingTest.Default.Run((latest, 4.795172810554504, 51.267413036466706, profile: bicycle),
                $"Snapping cold: wechelderzande5");
            var vorselaar1 = SnappingTest.Default.Run((latest, 4.7668540477752686, 51.23757128291549, profile: bicycle),
                $"Snapping cold: vorselaar1");
            var middelburg = SnappingTest.Default.Run((latest, 3.61363, 51.49967, profile: bicycle),
                $"Snapping cold: middelburg");
            var hermanTeirlinck = SnappingTest.Default.Run((latest, 4.35016, 50.86595, profile: bicycle),
                $"Snapping cold: herman teirlinck");
            hermanTeirlinck = SnappingTest.Default.Run((latest, 4.35016, 50.86595, profile: bicycle),
                $"Snapping hot: hermain teirlinck");
            var mechelenNeckerspoel = SnappingTest.Default.Run((latest, 4.48991060256958, 51.0298871358546, profile: bicycle),
                $"Snapping cold: mechelen neckerspoel");
            mechelenNeckerspoel = SnappingTest.Default.Run((latest, 4.48991060256958, 51.0298871358546, profile: bicycle),
                $"Snapping hot: mechelen neckerspoel");
            var dendermonde = SnappingTest.Default.Run((latest, 4.10142481327057, 51.0227846418863, profile: bicycle),
                $"Snapping cold: dendermonde");
            dendermonde = SnappingTest.Default.Run((latest, 4.10142481327057, 51.0227846418863, profile: bicycle),
                $"Snapping hot: dendermonde");
            var zellik1 = SnappingTest.Default.Run((latest, 4.27392840385437, 50.884507285755205, profile: bicycle),
                $"Snapping cold: zellik1"); 
            zellik1 = SnappingTest.Default.Run((latest, 4.27392840385437, 50.884507285755205, profile: bicycle),
                $"Snapping hot: zellik1"); 
            var zellik2 = SnappingTest.Default.Run((latest, 4.275886416435242, 50.88336336674239, profile: bicycle),
                $"Snapping cold: zellik2");
            zellik2 = SnappingTest.Default.Run((latest, 4.275886416435242, 50.88336336674239, profile: bicycle),
                $"Snapping hot: zellik2");
            var bruggeStation = SnappingTest.Default.Run((latest, 3.214899, 51.195129, profile: bicycle),
                $"Snapping cold: brugge-station");
            var stationDuinberge = SnappingTest.Default.Run((latest, 3.26358318328857, 51.3381990351222, profile: bicycle),
                $"Snapping cold: duinberge");
            var lesotho1 = SnappingTest.Default.Run((latest, 27.449684143066406, -30.147205394735842, profile: bicycle),
                $"Snapping cold: lesotho1");
            var lesotho2 = SnappingTest.Default.Run((latest, 27.464404106140133, -30.153625306867017, profile: bicycle),
                $"Snapping cold: lesotho2");

            Parallel.For(0, 10, (i) =>
            {
                SnappingTest.Default.Run((latest, 4.27392840385437, 50.884507285755205, profile: bicycle),
                    $"Snapping parallel: zellik1"); 
                SnappingTest.Default.Run((latest, 4.275886416435242, 50.88336336674239, profile: bicycle),
                    $"Snapping parallel: zellik2");
            });
            
            var route = RouterOneToOneTest.Default.Run((latest, lesotho1, lesotho2, bicycle),
                $"Route cold: {nameof(lesotho1)} -> {nameof(lesotho2)}");
            route = RouterOneToOneTest.Default.Run((latest, lesotho1, lesotho2, bicycle),
                $"Route hot: {nameof(lesotho1)} -> {nameof(lesotho2)}", 100);
            File.WriteAllText(Path.Combine("results", $"{nameof(lesotho1)}-{nameof(lesotho2)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
                $"Route cold: {nameof(zellik1)} -> {nameof(zellik2)}");
            route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
                $"Route hot: {nameof(zellik1)} -> {nameof(zellik2)}", 100);
            File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, wechelderzande1, vorselaar1, bicycle),
                $"Route cold: {nameof(wechelderzande1)} -> {nameof(vorselaar1)}");
            route = RouterOneToOneTest.Default.Run((latest, wechelderzande1, vorselaar1, bicycle),
                $"Route hot: {nameof(wechelderzande1)} -> {nameof(vorselaar1)}", 100);
            File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande1)}-{nameof(vorselaar1)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
                $"Route cold: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}"); 
            route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
                $"Route host: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}");
            File.WriteAllText(Path.Combine("results", $"{nameof(bruggeStation)}-{nameof(stationDuinberge)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
                $"Route cold: {nameof(zellik1)} -> {nameof(zellik2)}");
            route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
                $"Route hot: {nameof(zellik1)} -> {nameof(zellik2)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
                $"Route cold: {nameof(zellik2)} -> {nameof(zellik1)}");
            route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
                $"Route hot: {nameof(zellik2)} -> {nameof(zellik1)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(zellik2)}-{nameof(zellik1)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(ninove)}");
            route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(ninove)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}.geojson"), 
                route.ToGeoJson());
            
            Parallel.For(0, 10, (i) =>
            {
                RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
                    $"Routing parallel: {nameof(heldergem)} -> {nameof(ninove)}");
            });
            
            route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
                $"Route (after deserialization) cold: {nameof(zellik1)} -> {nameof(zellik2)}");
            route = RouterOneToOneTest.Default.Run((latest, zellik1, zellik2, bicycle),
                $"Route (after deserialization) hot: {nameof(zellik1)} -> {nameof(zellik2)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}-deserialized.geojson"), 
                route.ToGeoJson());
                
            route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
                $"Route (after deserialization) cold: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}"); 
            route = RouterOneToOneTest.Default.Run((latest, bruggeStation, stationDuinberge, bicycle),
                $"Route (after deserialization) host: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}");
            File.WriteAllText(Path.Combine("results", $"{nameof(bruggeStation)}-{nameof(stationDuinberge)}-deserialized.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
                $"Route (after deserialization) cold: {nameof(zellik2)} -> {nameof(zellik1)}");
            route = RouterOneToOneTest.Default.Run((latest, zellik2, zellik1, bicycle),
                $"Route (after deserialization) hot: {nameof(zellik2)} -> {nameof(zellik1)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(zellik2)}-{nameof(zellik1)}-deserialized.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
                $"Route (after deserialization) cold: {nameof(heldergem)} -> {nameof(ninove)}");
            route = RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
                $"Route (after deserialization) hot: {nameof(heldergem)} -> {nameof(ninove)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}-deserialized.geojson"), 
                route.ToGeoJson());
            
            Parallel.For(0, 10, (i) =>
            {
                RouterOneToOneTest.Default.Run((latest, heldergem, ninove, bicycle),
                    $"Routing (after deserialization) parallel: {nameof(heldergem)} -> {nameof(ninove)}");
            });
            
            route = RouterOneToOneTest.Default.Run((latest, heldergem, pepingen, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(pepingen)}");
            route = RouterOneToOneTest.Default.Run((latest, heldergem, pepingen, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(pepingen)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(pepingen)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, heldergem, lebbeke, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(lebbeke)}");
            route = RouterOneToOneTest.Default.Run((latest, heldergem, lebbeke, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(lebbeke)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(lebbeke)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, heldergem, stekene, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(stekene)}");
            route = RouterOneToOneTest.Default.Run((latest, heldergem, stekene, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(stekene)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(stekene)}.geojson"), 
                route.ToGeoJson());
            
            route = RouterOneToOneTest.Default.Run((latest, heldergem, hamme, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(hamme)}");
            route = RouterOneToOneTest.Default.Run((latest, heldergem, hamme, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(hamme)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(hamme)}.geojson"), 
                route.ToGeoJson());

            route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande5, DirectionEnum.East),
                (wechelderzande2, DirectionEnum.West), bicycle));
            File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.East)}-" +
                                                      $"{nameof(wechelderzande2)}_{nameof(DirectionEnum.West)}.geojson"),route.ToGeoJson());
            route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande5, DirectionEnum.East),
                (wechelderzande2, null), bicycle));
            File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.East)}-" +
                                                      $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());
            route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande5, DirectionEnum.West),
                (wechelderzande2, null), bicycle));
            File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.West)}-" +
                                                      $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());

            route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande4, DirectionEnum.South),
                (wechelderzande2, null), bicycle));
            File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande4)}_{nameof(DirectionEnum.South)}-" +
                                                      $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());
            route = RouterOneToOneDirectedTest.Default.Run((latest, (wechelderzande4, DirectionEnum.North),
                (wechelderzande2, null), bicycle));
            File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande4)}_{nameof(DirectionEnum.North)}-" +
                                                      $"{nameof(wechelderzande2)}.geojson"),route.ToGeoJson());

            var oneToManyRoutes = RouterOneToManyTest.Default.Run(
                (latest, heldergem, new[] {ninove, pepingen, lebbeke}, bicycle),
                $"Routes (one to many) cold: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            oneToManyRoutes = RouterOneToManyTest.Default.Run(
                (latest, heldergem, new[] {ninove, pepingen, lebbeke}, bicycle),
                $"Routes (one to many) hot: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-0.geojson"),
                oneToManyRoutes[0].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
                oneToManyRoutes[1].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-2.geojson"),
                oneToManyRoutes[2].ToGeoJson());
            
            // tests for many to one routing.
            var manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, new [] {ninove, pepingen, lebbeke}, heldergem, bicycle),
                $"Routes (many to one) cold: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
            manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, new [] {ninove, pepingen, lebbeke}, heldergem, bicycle),
                $"Routes (many to one) hot: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
            File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-0.geojson"),
                manyToOneRoutes[0].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-1.geojson"),
                manyToOneRoutes[1].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-2.geojson"),
                manyToOneRoutes[2].ToGeoJson());
            
            manyToOneRoutes = RouterOneToManyTest.Default.Run((latest, heldergem, new [] {ninove, pepingen, lebbeke}, bicycle),
                $"Routes (one to many) cold: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            manyToOneRoutes = RouterOneToManyTest.Default.Run((latest, heldergem, new [] {ninove, pepingen, lebbeke}, bicycle),
                $"Routes (one to many) hot: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-0.geojson"),
                manyToOneRoutes[0].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
                manyToOneRoutes[1].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
                manyToOneRoutes[2].ToGeoJson());

            for (var j = 0; j < 5; j++)
            {
                // setup a router db with a routable tiles data provider.
                routerDb = new RouterDb(new RouterDbConfiguration()
                {
                    Zoom = 14
                });
                routerDb.Mutate(mutable =>
                {
                    mutable.PrepareFor(bicycle);
                });
                routerDb.UseRouteableTiles(s =>
                {
                    s.Url = "https://data1.anyways.eu/tiles/20200527-080000";
                });
                latest = routerDb.Network;

                var deSterre = SnappingTest.Default.Run((latest, 3.715675, 51.026164, profile: bicycle),
                    $"Snapping cold: deSterre");

                var targets = new[]
                {
                    SnappingTest.Default.Run((latest, 3.70137870311737, 51.1075870861261, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.7190705537796, 51.0883577942415, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.74058723449707, 51.0563671057799, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.65520179271698, 51.0472956366036, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.71066987514496, 51.0358985842182, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.75632107257843, 51.0386479278862, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.76888453960419, 51.0175340229811, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.79708528518677, 51.0028081059898, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.65046501159668, 50.9970656297241, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.82958829402924, 50.9917511038545, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.80167186260223, 50.9801985244791, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.62783789634705, 50.960774752016, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.79114151000977, 50.9705486324505, profile: bicycle),
                        $"Snapping cold."),
                    SnappingTest.Default.Run((latest, 3.78133535385132, 50.9535197402615, profile: bicycle),
                        $"Snapping cold."),
                };
                
                Parallel.For(0, 10, (i) =>
                {
                    manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, targets, deSterre, bicycle),
                        $"Route cold: many to one to {nameof(deSterre)}");
                    manyToOneRoutes = RouterManyToOneTest.Default.Run((latest, targets, deSterre, bicycle),
                        $"Route hot: many to one to {nameof(deSterre)}");
                });
            }
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
            Logger.LogAction = (o, level, message, parameters) =>
            {
                if (loggingBlacklist.Contains(o))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(o))
                {
                    message = $"[{o}] {message}";
                }

                if (level == TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug(message);
                }
                else if (level == TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information(message);
                }
                else if (level == TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning(message);
                }
                else if (level == TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal(message);
                }
                else if (level == TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error(message);
                }
                else
                {
                    Log.Debug(message);
                }
            };
        }
    }
}
