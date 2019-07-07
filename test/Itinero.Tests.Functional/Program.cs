using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Algorithms.Dijkstra;
using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Itinero.IO.Json;
using Itinero.IO.Osm.Tiles;
using Itinero.IO.Osm.Tiles.Parsers;
using Itinero.IO.Shape;
using Itinero.LocalGeo;
using Itinero.Logging;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using Itinero.Tests.Functional.Staging;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using OsmSharp;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

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

            var heldergem = SnappingTest.Default.Run((routerDb, 3.95454, 50.88142, profile: bicycle),
                $"Snapping cold: heldergem");
            heldergem = SnappingTest.Default.Run((routerDb, 3.95454, 50.88142, profile: bicycle),
                $"Snapping hot: heldergem");
            var ninove = SnappingTest.Default.Run((routerDb, 4.02573, 50.83963, profile: bicycle));
            var pepingen = SnappingTest.Default.Run((routerDb, 4.15887, 50.75932, profile: bicycle));

            var route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, ninove, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(ninove)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, ninove, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(ninove)}");

            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, pepingen, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(pepingen)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, pepingen, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(pepingen)}");
            
            File.WriteAllText("network.geojson", routerDb.ToGeoJson());
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
                    //Log.Debug(message);
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
