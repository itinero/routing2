using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Geo.Directions;
using Itinero.IO.Json.GeoJson;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles.Parsers;
using Itinero.Network.Search.Islands;
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

namespace Itinero.Tests.Functional;

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
        var routerDb = RouterDb.ReadFrom(routerDbStream);
        routerDb.EdgeTypeMap = new OsmEdgeTypeMap();
        return routerDb;
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
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });

        routerDb.PrepareFor(p);
        using var osmStream = File.OpenRead(Staging.Download.Get(localFile, url));
        var progress = new OsmStreamFilterProgress();
        var osmPbfStream = new PBFOsmStreamSource(osmStream);
        progress.RegisterSource(osmPbfStream);

        routerDb.UseOsmData(progress);

        return routerDb;
    }

    private static RouterDb GetOrCreate(Profile p, string url, string localFile = "latest.osm.pbf")
    {
        var routerDbFile = Path.ChangeExtension(localFile, ".routerdb");
        if (File.Exists(routerDbFile))
        {
            return FromFile(routerDbFile);
        }

        var routerDb = FromUrl(p, url, localFile);
        ToFile(routerDbFile, routerDb);
        return routerDb;
    }


    private static async Task Main(string[] args)
    {
        EnableLogging();

        TileParser.DownloadFunc = DownloadHelper.Download;

        //var routerDb = RouterDb.ReadFrom(File.OpenRead("data.routerdb")); 

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

        var car = Profiles.Lua.Osm.OsmProfiles.Car;

        // // setup a router db with a local osm file (cached as .routerdb).
        // var routerDb = GetOrCreate(car, LuxembourgUrl, "luxembourg-latest.osm.pbf");
        // routerDb.PrepareFor(car);
        //
        // var lux1 = (5.99620407852791,
        //     49.673960512047614, (float?)0f);
        // var lux2 = (6.124148368835449, 49.588792167215345, (float?)0f);
        //
        // var latest = routerDb.Latest;
        // var lux1sp = await latest.Snap(car).ToAsync(lux1);
        // var lux2sp = await latest.Snap(car).ToAsync(lux2);

        // latest.Islands(s =>
        // {
        //     s.Profile = car;
        // }).IsOnIsland(lux1sp.Value.EdgeId, true);

        // var oneToOne = await RouterOneToOneTest.Default.RunAsync((latest, lux1sp, lux2sp, car));
        // var oneToOneGeoJson = oneToOne.ToGeoJson();
        // var routes = await RouterOneToOneWithAlternativeTest.Default.RunAsync(
        //     (latest, lux1sp, lux2sp, car)
        // );
        //
        // var geoJson = routes.Select(r => r.ToGeoJson()).ToList();
        // Console.WriteLine(geoJson);

        // === Belgium tests ===
        Directory.CreateDirectory("results");

        var routerDb = GetOrCreate(car, BelgiumUrl, "belgium-latest.osm.pbf");
        routerDb.PrepareFor(car);
        var latest = routerDb.Latest;

        // snap all Belgium test points
        var zellik1 = await SnappingTest.Default.RunAsync(
            (latest, 4.27392840385437, 50.884507285755205, car), "Snapping: zellik1");
        var zellik2 = await SnappingTest.Default.RunAsync(
            (latest, 4.275886416435242, 50.88336336674239, car), "Snapping: zellik2");

        // one-to-one routing tests
        var route = await RouterOneToOneTest.Default.RunAsync((latest, zellik1, zellik2, car),
            $"Route cold: {nameof(zellik1)} -> {nameof(zellik2)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, zellik1, zellik2, car),
            $"Route hot: {nameof(zellik1)} -> {nameof(zellik2)}", 100);
        File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}.geojson"),
            route.ToGeoJson());

        var wechelderzande1 = await SnappingTest.Default.RunAsync(
            (latest, 4.80129, 51.26774, car), "Snapping: wechelderzande1");
        var wechelderzande2 = await SnappingTest.Default.RunAsync(
            (latest, 4.794577360153198, 51.26723850107129, car), "Snapping: wechelderzande2");
        var wechelderzande4 = await SnappingTest.Default.RunAsync(
            (latest, 4.796256422996521, 51.261015209797186, car), "Snapping: wechelderzande4");
        var wechelderzande5 = await SnappingTest.Default.RunAsync(
            (latest, 4.795172810554504, 51.267413036466706, car), "Snapping: wechelderzande5");
        var vorselaar1 = await SnappingTest.Default.RunAsync(
            (latest, 4.7668540477752686, 51.23757128291549, car), "Snapping: vorselaar1");

        route = await RouterOneToOneTest.Default.RunAsync((latest, wechelderzande1, vorselaar1, car),
            $"Route cold: {nameof(wechelderzande1)} -> {nameof(vorselaar1)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, wechelderzande1, vorselaar1, car),
            $"Route hot: {nameof(wechelderzande1)} -> {nameof(vorselaar1)}", 100);
        File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande1)}-{nameof(vorselaar1)}.geojson"),
            route.ToGeoJson());

        var bruggeStation = await SnappingTest.Default.RunAsync(
            (latest, 3.214899, 51.195129, car), "Snapping: brugge-station");
        var stationDuinberge = await SnappingTest.Default.RunAsync(
            (latest, 3.26358318328857, 51.3381990351222, car), "Snapping: duinberge");
        var stekene = await SnappingTest.Default.RunAsync(
            (latest, 4.03705, 51.20637, car), "Snapping: stekene");
        var heldergem = await SnappingTest.Default.RunAsync(
            (latest, 3.93702, 50.88283, car), "Snapping: heldergem");
        var ninove = await SnappingTest.Default.RunAsync(
            (latest, 4.02486, 50.83536, car), "Snapping: ninove");
        var pepingen = await SnappingTest.Default.RunAsync(
            (latest, 4.15410, 50.76274, car), "Snapping: pepingen");
        var lebbeke = await SnappingTest.Default.RunAsync(
            (latest, 4.13916, 51.00328, car), "Snapping: lebbeke");
        var hamme = await SnappingTest.Default.RunAsync(
            (latest, 4.13371, 51.09755, car), "Snapping: hamme");



        route = await RouterOneToOneTest.Default.RunAsync((latest, bruggeStation, stationDuinberge, car),
            $"Route cold: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, bruggeStation, stationDuinberge, car),
            $"Route hot: {nameof(bruggeStation)} -> {nameof(stationDuinberge)}");
        File.WriteAllText(Path.Combine("results", $"{nameof(bruggeStation)}-{nameof(stationDuinberge)}.geojson"),
            route.ToGeoJson());

        route = await RouterOneToOneTest.Default.RunAsync((latest, zellik1, zellik2, car),
            $"Route cold: {nameof(zellik1)} -> {nameof(zellik2)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, zellik1, zellik2, car),
            $"Route hot: {nameof(zellik1)} -> {nameof(zellik2)}", 10);
        File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}.geojson"),
            route.ToGeoJson());

        route = await RouterOneToOneTest.Default.RunAsync((latest, zellik2, zellik1, car),
            $"Route cold: {nameof(zellik2)} -> {nameof(zellik1)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, zellik2, zellik1, car),
            $"Route hot: {nameof(zellik2)} -> {nameof(zellik1)}", 10);
        File.WriteAllText(Path.Combine("results", $"{nameof(zellik2)}-{nameof(zellik1)}.geojson"),
            route.ToGeoJson());

        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, ninove, car),
            $"Route cold: {nameof(heldergem)} -> {nameof(ninove)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, ninove, car),
            $"Route hot: {nameof(heldergem)} -> {nameof(ninove)}", 10);
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}.geojson"),
            route.ToGeoJson());

        await Parallel.ForEachAsync(Enumerable.Range(0, 10), async (_, _) =>
        {
            await RouterOneToOneTest.Default.RunAsync((latest, heldergem, ninove, car),
                $"Routing parallel: {nameof(heldergem)} -> {nameof(ninove)}");
        });

        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, pepingen, car),
            $"Route cold: {nameof(heldergem)} -> {nameof(pepingen)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, pepingen, car),
            $"Route hot: {nameof(heldergem)} -> {nameof(pepingen)}", 10);
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(pepingen)}.geojson"),
            route.ToGeoJson());

        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, lebbeke, car),
            $"Route cold: {nameof(heldergem)} -> {nameof(lebbeke)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, lebbeke, car),
            $"Route hot: {nameof(heldergem)} -> {nameof(lebbeke)}", 10);
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(lebbeke)}.geojson"),
            route.ToGeoJson());

        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, stekene, car),
            $"Route cold: {nameof(heldergem)} -> {nameof(stekene)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, stekene, car),
            $"Route hot: {nameof(heldergem)} -> {nameof(stekene)}", 10);
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(stekene)}.geojson"),
            route.ToGeoJson());

        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, hamme, car),
            $"Route cold: {nameof(heldergem)} -> {nameof(hamme)}");
        route = await RouterOneToOneTest.Default.RunAsync((latest, heldergem, hamme, car),
            $"Route hot: {nameof(heldergem)} -> {nameof(hamme)}", 10);
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(hamme)}.geojson"),
            route.ToGeoJson());

        // directed routing tests
        route = await RouterOneToOneDirectedTest.Default.RunAsync((latest, (wechelderzande5, (DirectionEnum?)DirectionEnum.East),
            (wechelderzande2, (DirectionEnum?)DirectionEnum.West), car));
        File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.East)}-" +
                                                   $"{nameof(wechelderzande2)}_{nameof(DirectionEnum.West)}.geojson"), route.ToGeoJson());
        route = await RouterOneToOneDirectedTest.Default.RunAsync((latest, (wechelderzande5, (DirectionEnum?)DirectionEnum.East),
            (wechelderzande2, (DirectionEnum?)null), car));
        File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.East)}-" +
                                                   $"{nameof(wechelderzande2)}.geojson"), route.ToGeoJson());
        route = await RouterOneToOneDirectedTest.Default.RunAsync((latest, (wechelderzande5, (DirectionEnum?)DirectionEnum.West),
            (wechelderzande2, (DirectionEnum?)null), car));
        File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande5)}_{nameof(DirectionEnum.West)}-" +
                                                   $"{nameof(wechelderzande2)}.geojson"), route.ToGeoJson());

        route = await RouterOneToOneDirectedTest.Default.RunAsync((latest, (wechelderzande4, (DirectionEnum?)DirectionEnum.South),
            (wechelderzande2, (DirectionEnum?)null), car));
        File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande4)}_{nameof(DirectionEnum.South)}-" +
                                                   $"{nameof(wechelderzande2)}.geojson"), route.ToGeoJson());
        route = await RouterOneToOneDirectedTest.Default.RunAsync((latest, (wechelderzande4, (DirectionEnum?)DirectionEnum.North),
            (wechelderzande2, (DirectionEnum?)null), car));
        File.WriteAllText(Path.Combine("results", $"{nameof(wechelderzande4)}_{nameof(DirectionEnum.North)}-" +
                                                   $"{nameof(wechelderzande2)}.geojson"), route.ToGeoJson());

        // one-to-many routing tests
        var oneToManyRoutes = await RouterOneToManyTest.Default.RunAsync(
            (latest, heldergem, new[] { ninove, pepingen, lebbeke }, car),
            $"Routes (one to many) cold: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
        oneToManyRoutes = await RouterOneToManyTest.Default.RunAsync(
            (latest, heldergem, new[] { ninove, pepingen, lebbeke }, car),
            $"Routes (one to many) hot: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-0.geojson"),
            oneToManyRoutes[0].ToGeoJson());
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
            oneToManyRoutes[1].ToGeoJson());
        File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-2.geojson"),
            oneToManyRoutes[2].ToGeoJson());

        // many-to-one routing tests
        var manyToOneRoutes = await RouterManyToOneTest.Default.RunAsync(
            (latest, new[] { ninove, pepingen, lebbeke }, heldergem, car),
            $"Routes (many to one) cold: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
        manyToOneRoutes = await RouterManyToOneTest.Default.RunAsync(
            (latest, new[] { ninove, pepingen, lebbeke }, heldergem, car),
            $"Routes (many to one) hot: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
        File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-0.geojson"),
            manyToOneRoutes[0].ToGeoJson());
        File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-1.geojson"),
            manyToOneRoutes[1].ToGeoJson());
        File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-2.geojson"),
            manyToOneRoutes[2].ToGeoJson());
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

        Logging.Logger.LogAction = (o, level, message, parameters) =>
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
