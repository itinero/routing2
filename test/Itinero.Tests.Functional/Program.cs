using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.IO.Json.GeoJson;
using Itinero.IO.Osm;

using Itinero.Profiles;
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

        var car = Profiles.Lua.Osm.OsmProfiles.Car;

        // === Belgium tests ===
        Directory.CreateDirectory("results");

        var routerDb = GetOrCreate(car, BelgiumUrl, "belgium-latest.osm.pbf");
        routerDb.PrepareFor(car);
        var latest = routerDb.Latest;

        // all test locations
        var locations = new (string name, double longitude, double latitude)[]
        {
            ("zellik1", 4.27392840385437, 50.884507285755205),
            ("zellik2", 4.275886416435242, 50.88336336674239),
            ("wechelderzande1", 4.80129, 51.26774),
            ("wechelderzande2", 4.794577360153198, 51.26723850107129),
            ("wechelderzande4", 4.796256422996521, 51.261015209797186),
            ("wechelderzande5", 4.795172810554504, 51.267413036466706),
            ("vorselaar1", 4.7668540477752686, 51.23757128291549),
            ("bruggeStation", 3.214899, 51.195129),
            ("stationDuinberge", 3.26358318328857, 51.3381990351222),
            ("stekene", 4.03705, 51.20637),
            ("heldergem", 3.93702, 50.88283),
            ("ninove", 4.02486, 50.83536),
            ("pepingen", 4.15410, 50.76274),
            ("lebbeke", 4.13916, 51.00328),
            ("hamme", 4.13371, 51.09755)
        };

        // snap all test locations (cold + hot)
        var snapPoints = new Dictionary<string, SnapPoint>();
        foreach (var (name, longitude, latitude) in locations)
        {
            await SnappingTest.Default.RunAsync(
                (latest, longitude, latitude, car), $"Snapping cold: {name}");
            var sp = await SnappingTest.Default.RunAsync(
                (latest, longitude, latitude, car), $"Snapping hot: {name}");
            snapPoints[name] = sp;
        }

        // route between all pairs
        var failed = new List<(string from, string to)>();
        for (var i = 0; i < locations.Length; i++)
        {
            for (var j = 0; j < locations.Length; j++)
            {
                if (i == j) continue;

                var from = locations[i].name;
                var to = locations[j].name;

                try
                {
                    var route = await RouterOneToOneTest.Default.RunAsync(
                        (latest, snapPoints[from], snapPoints[to], car),
                        $"Route: {from} -> {to}");
                    File.WriteAllText(
                        Path.Combine("results", $"{from}-{to}.geojson"),
                        route.ToGeoJson());
                }
                catch (Exception ex)
                {
                    failed.Add((from, to));
                    Log.Warning($"Route failed: {from} -> {to}: {ex.Message}");
                }
            }
        }

        if (failed.Count > 0)
        {
            Log.Warning($"{failed.Count} routes failed:");
            foreach (var (from, to) in failed)
            {
                Log.Warning($"  {from} -> {to}");
            }
        }

        // parallel routing tests
        var allSnapPoints = locations.Select(l => snapPoints[l.name]).ToArray();
        await Parallel.ForEachAsync(Enumerable.Range(0, 10), async (_, _) =>
        {
            try
            {
                await RouterOneToOneTest.Default.RunAsync(
                    (latest, allSnapPoints[0], allSnapPoints[^1], car),
                    $"Route parallel: {locations[0].name} -> {locations[^1].name}");
            }
            catch (Exception)
            {
                // already reported in sequential run
            }
        });

        // one-to-many routing tests (each location to all others)
        for (var i = 0; i < locations.Length; i++)
        {
            var source = allSnapPoints[i];
            var targets = allSnapPoints.Where((_, idx) => idx != i).ToArray();
            var targetNames = locations.Where((_, idx) => idx != i).Select(l => l.name).ToArray();

            try
            {
                var oneToManyRoutes = await RouterOneToManyTest.Default.RunAsync(
                    (latest, source, targets, car),
                    $"Routes (one to many): {locations[i].name} -> {string.Join(",", targetNames)}");
                for (var r = 0; r < oneToManyRoutes.Length; r++)
                {
                    File.WriteAllText(
                        Path.Combine("results", $"one-to-many-{locations[i].name}-{targetNames[r]}.geojson"),
                        oneToManyRoutes[r].ToGeoJson());
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"One-to-many failed from {locations[i].name}: {ex.Message}");
            }
        }

        // many-to-one routing tests (all others to each location)
        for (var i = 0; i < locations.Length; i++)
        {
            var target = allSnapPoints[i];
            var sources = allSnapPoints.Where((_, idx) => idx != i).ToArray();
            var sourceNames = locations.Where((_, idx) => idx != i).Select(l => l.name).ToArray();

            try
            {
                var manyToOneRoutes = await RouterManyToOneTest.Default.RunAsync(
                    (latest, sources, target, car),
                    $"Routes (many to one): {string.Join(",", sourceNames)} -> {locations[i].name}");
                for (var r = 0; r < manyToOneRoutes.Length; r++)
                {
                    File.WriteAllText(
                        Path.Combine("results", $"many-to-one-{sourceNames[r]}-{locations[i].name}.geojson"),
                        manyToOneRoutes[r].ToGeoJson());
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Many-to-one failed to {locations[i].name}: {ex.Message}");
            }
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
