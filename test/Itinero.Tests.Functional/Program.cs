using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Itinero.Algorithms.Dijkstra;
using Itinero.Data.Attributes;
using Itinero.Data.Graphs;
using Itinero.Data.Graphs.Coders;
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
using Attribute = Itinero.Data.Attributes.Attribute;

namespace Itinero.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            EnableLogging();
            
            // do some local caching.
            TileParser.DownloadFunc = Download.DownloadHelper.Download;

            var bicycle = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
            var pedestrian = Itinero.Profiles.Lua.Osm.OsmProfiles.Pedestrian;
            //var bicycle = LuaProfile.Load(File.ReadAllText(@"bicycle.lua"));
            
            // setup a router db with a routable tiles data provider.
            var routerDb = new RouterDb(new RouterDbConfiguration()
            {
                Zoom = 14,
                EdgeDataLayout = new EdgeDataLayout(new (string key, EdgeDataType dataType)[]
                {
                    ("bicycle.weight", EdgeDataType.UInt32),
                    ("pedestrian.weight", EdgeDataType.UInt32)
                })
            });
            routerDb.DataProvider = new DataProvider(routerDb);

            var factor = bicycle.Factor(new AttributeCollection(
                new Attribute("highway", "pedestrian")));

            factor = bicycle.Factor(new AttributeCollection(
                new Attribute("highway", "pedestrian"),
                new Attribute("surface", "cobblestone")));

            var heldergem = SnappingTest.Default.Run((routerDb, 3.95454, 50.88142, profile: bicycle),
                $"Snapping cold: heldergem");
            heldergem = SnappingTest.Default.Run((routerDb, 3.95454, 50.88142, profile: bicycle),
                $"Snapping hot: heldergem", 1000);
            var ninove = SnappingTest.Default.Run((routerDb, 4.02573, 50.83963, profile: bicycle),
                $"Snapping hot: ninove");
            var pepingen = SnappingTest.Default.Run((routerDb, 4.15887, 50.75932, profile: bicycle),
                $"Snapping hot: pepingen");
            var lebbeke = SnappingTest.Default.Run((routerDb, 4.12864, 50.99926, profile: bicycle),
                $"Snapping cold: lebbeke");
            var hamme = SnappingTest.Default.Run((routerDb, 4.13418, 51.09707, profile: bicycle),
                $"Snapping cold: hamme");
            var stekene = SnappingTest.Default.Run((routerDb, 4.03705, 51.20637, profile: bicycle),
                $"Snapping cold: hamme");
            var leuven = SnappingTest.Default.Run((routerDb, 4.69575, 50.88040, profile: bicycle),
                $"Snapping cold: hamme");
            var wechelderzande = SnappingTest.Default.Run((routerDb, 4.80129, 51.26774, profile: bicycle),
                $"Snapping cold: wechelderzande");
            var middelburg = SnappingTest.Default.Run((routerDb, 3.61363, 51.49967, profile: bicycle),
                $"Snapping cold: middelburg");
            var hermanTeirlinck = SnappingTest.Default.Run((routerDb, 4.35016, 50.86595, profile: bicycle),
                $"Snapping cold: hermain teirlinck");
            hermanTeirlinck = SnappingTest.Default.Run((routerDb, 4.35016, 50.86595, profile: bicycle),
                $"Snapping hot: hermain teirlinck");
            var mechelenNeckerspoel = SnappingTest.Default.Run((routerDb, 4.48991060256958, 51.0298871358546, profile: bicycle),
                $"Snapping cold: mechelen neckerspoel");
            mechelenNeckerspoel = SnappingTest.Default.Run((routerDb, 4.48991060256958, 51.0298871358546, profile: bicycle),
                $"Snapping hot: mechelen neckerspoel");
            var dendermonde = SnappingTest.Default.Run((routerDb, 4.10142481327057, 51.0227846418863, profile: bicycle),
                $"Snapping cold: dendermonde");
            dendermonde = SnappingTest.Default.Run((routerDb, 4.10142481327057, 51.0227846418863, profile: bicycle),
                $"Snapping hot: dendermonde");
            var zellik1 =SnappingTest.Default.Run((routerDb, 4.27392840385437, 50.884507285755205, profile: bicycle),
                $"Snapping hot: zellik1"); 
            var zellik2 =SnappingTest.Default.Run((routerDb, 4.275886416435242, 50.88336336674239, profile: bicycle),
                $"Snapping hot: zellik2");

            Parallel.For(0, 10, (i) =>
            {
                SnappingTest.Default.Run((routerDb, 4.27392840385437, 50.884507285755205, profile: bicycle),
                    $"Snapping parallel: zellik1"); 
                SnappingTest.Default.Run((routerDb, 4.275886416435242, 50.88336336674239, profile: bicycle),
                    $"Snapping parallel: zellik2");
            });
            
            var routes = ManyToOneTest.Default.Run((routerDb, new [] {ninove, pepingen, lebbeke}, heldergem, bicycle),
                $"Routes (many to one) cold: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
            routes = ManyToOneTest.Default.Run((routerDb, new [] {ninove, pepingen, lebbeke}, heldergem, bicycle),
                $"Routes (many to one) hot: {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)} -> {nameof(heldergem)}");
            File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-0.geojson"),
                routes[0].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-1.geojson"),
                routes[1].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-{nameof(heldergem)}-2.geojson"),
                routes[2].ToGeoJson());
            
            routes = OneToManyTest.Default.Run((routerDb, heldergem, new [] {ninove, pepingen, lebbeke}, bicycle),
                $"Routes (one to many) cold: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            routes = OneToManyTest.Default.Run((routerDb, heldergem, new [] {ninove, pepingen, lebbeke}, bicycle),
                $"Routes (one to many) hot: {nameof(heldergem)} -> {nameof(ninove)},{nameof(pepingen)},{nameof(lebbeke)}");
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-0.geojson"),
                routes[0].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
                routes[1].ToGeoJson());
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}_{nameof(pepingen)}_{nameof(lebbeke)}-1.geojson"),
                routes[2].ToGeoJson());
            
            var route = PointToPointRoutingTest.Default.Run((routerDb, zellik1, zellik2, bicycle),
                $"Route cold: {nameof(zellik1)} -> {nameof(zellik2)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, zellik1, zellik2, bicycle),
                $"Route hot: {nameof(zellik1)} -> {nameof(zellik2)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(zellik1)}-{nameof(zellik2)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, zellik2, zellik1, bicycle),
                $"Route cold: {nameof(zellik2)} -> {nameof(zellik1)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, zellik2, zellik1, bicycle),
                $"Route hot: {nameof(zellik2)} -> {nameof(zellik1)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(zellik2)}-{nameof(zellik1)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, ninove, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(ninove)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, ninove, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(ninove)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(ninove)}.geojson"), 
                route.ToGeoJson());

            Parallel.For(0, 10, (i) =>
            {
                PointToPointRoutingTest.Default.Run((routerDb, heldergem, ninove, bicycle),
                    $"Routing parallel: {nameof(heldergem)} -> {nameof(ninove)}");
            });
            
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, pepingen, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(pepingen)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, pepingen, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(pepingen)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(pepingen)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, lebbeke, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(lebbeke)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, lebbeke, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(lebbeke)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(lebbeke)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, stekene, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(stekene)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, stekene, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(stekene)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(stekene)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, hamme, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(hamme)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, hamme, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(hamme)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(hamme)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, leuven, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(leuven)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, leuven, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(leuven)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(heldergem)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, wechelderzande, bicycle),
                $"Route cold: {nameof(heldergem)} -> {nameof(wechelderzande)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, heldergem, wechelderzande, bicycle),
                $"Route hot: {nameof(heldergem)} -> {nameof(wechelderzande)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(heldergem)}-{nameof(wechelderzande)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, hermanTeirlinck, mechelenNeckerspoel, bicycle),
                $"Route cold: {nameof(hermanTeirlinck)} -> {nameof(mechelenNeckerspoel)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, hermanTeirlinck, mechelenNeckerspoel, bicycle),
                $"Route hot: {nameof(hermanTeirlinck)} -> {nameof(mechelenNeckerspoel)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(hermanTeirlinck)}-{nameof(mechelenNeckerspoel)}.geojson"), 
                route.ToGeoJson());
            
            route = PointToPointRoutingTest.Default.Run((routerDb, hermanTeirlinck, dendermonde, bicycle),
                $"Route cold: {nameof(hermanTeirlinck)} -> {nameof(dendermonde)}");
            route = PointToPointRoutingTest.Default.Run((routerDb, hermanTeirlinck, dendermonde, bicycle),
                $"Route hot: {nameof(hermanTeirlinck)} -> {nameof(dendermonde)}", 10);
            File.WriteAllText(Path.Combine("results", $"{nameof(hermanTeirlinck)}-{nameof(dendermonde)}.geojson"), 
                route.ToGeoJson());
            
            routerDb.WriteToShape("test");
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
