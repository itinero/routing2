using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Itinero;
using Itinero.Geo;
using Itinero.IO.Osm;
using Itinero.Network;
using Itinero.Network.Search.Edges;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Routing.Costs;
using Itinero.Snapping;
using OsmSharp.Streams;

namespace Itinero.Tests.Benchmarks;

internal static class SnapDiagnostic
{
    internal static void Run()
    {
        var car = OsmProfiles.Car;

        Console.WriteLine("Loading Belgium from PBF...");
        var routerDb = new RouterDb(new RouterDbConfiguration { Zoom = 14 });
        routerDb.PrepareFor(car);

        var pbfPath = "belgium-latest.osm.pbf";
        if (!File.Exists(pbfPath))
        {
            pbfPath = Path.Combine("test", "Itinero.Tests.Functional", "belgium-latest.osm.pbf");
        }

        if (!File.Exists(pbfPath))
        {
            Console.WriteLine($"Cannot find {pbfPath}");
            return;
        }

        using (var osmStream = File.OpenRead(pbfPath))
        {
            routerDb.UseOsmData(new PBFOsmStreamSource(osmStream));
        }

        var network = routerDb.Latest;

        var lon = 4.80129;
        var lat = 51.26774;

        // Reproduce exact functional test sequence
        Console.WriteLine("\nExact functional test sequence...");

        var zellik1 = network.Snap(car).ToAsync(4.27392840385437, 50.884507285755205).Result;
        Console.WriteLine($"  zellik1 snap: {(zellik1.IsError ? "FAILED" : "OK")}");
        var zellik2 = network.Snap(car).ToAsync(4.275886416435242, 50.88336336674239).Result;
        Console.WriteLine($"  zellik2 snap: {(zellik2.IsError ? "FAILED" : "OK")}");

        if (!zellik1.IsError && !zellik2.IsError)
        {
            var route = network.Route(car).From(zellik1.Value).To(zellik2.Value).CalculateAsync().Result;
            Console.WriteLine($"  zellik cold route: {(route.IsError ? "FAILED" : "OK")}");

            for (var i = 0; i < 100; i++)
                network.Route(car).From(zellik1.Value).To(zellik2.Value).CalculateAsync().Wait();
            Console.WriteLine($"  zellik hot routes: done");
        }

        var w1 = network.Snap(car).ToAsync(lon, lat).Result;
        Console.WriteLine($"  wechelderzande after zellik sequence: {(w1.IsError ? "FAILED" : "OK")}");

        // Try snap without CheckCanStopOn
        var snapNoStop = network.Snap(car, s => s.CheckCanStopOn = false).ToAsync(lon, lat).Result;
        Console.WriteLine($"  Snap with CheckCanStopOn=false: {(snapNoStop.IsError ? "FAILED" : "OK")}");

        // Try snap without any profile checks
        var snapNoProfile = network.Snap(Array.Empty<Profile>()).ToAsync(lon, lat).Result;
        Console.WriteLine($"  Snap with no profiles: {(snapNoProfile.IsError ? "FAILED" : "OK")}");
        if (!snapNoProfile.IsError)
        {
            Console.WriteLine($"    -> EdgeId={snapNoProfile.Value.EdgeId}, Offset={snapNoProfile.Value.Offset}");
        }

        // Try with larger max distance
        var snapLarge = network.Snap(car, s => { s.MaxDistance = 10000; s.OffsetInMeter = 1000; s.OffsetInMeterMax = 2000; }).ToAsync(lon, lat).Result;
        Console.WriteLine($"  Snap with maxDist=10km: {(snapLarge.IsError ? "FAILED" : "OK")}");
        if (!snapLarge.IsError)
        {
            var loc = snapLarge.Value.LocationOnNetwork(network);
            var dist = ((double, double, float? e))(lon, lat, null);
            Console.WriteLine($"    -> EdgeId={snapLarge.Value.EdgeId} location=({loc.longitude:F5},{loc.latitude:F5})");
        }

        // Check what the snapper's edge search returns
        Console.WriteLine("\n  Edge search in snap box after routing:");
        var snapBox = ((double longitude, double latitude, float? e))(lon, lat, null);
        var searchEdges = network.SearchEdgesInBox(snapBox.BoxAround(500));
        var foundInBox = 0;
        while (searchEdges.MoveNext())
        {
            foundInBox++;
            if (foundInBox <= 3)
            {
                Console.WriteLine($"    Found: EdgeId={searchEdges.EdgeId} Forward={searchEdges.Forward}");
            }
        }
        Console.WriteLine($"    Total edges in 500m box: {foundInBox}");

        // Check cost function results for edges near wechelderzande
        Console.WriteLine("\n  Cost function check for nearby edges:");
        var costFunc = network.GetCostFunctionFor(car);
        var edgeEnum2 = network.GetEdgeEnumerator();
        var vertEnum2 = network.GetVertexEnumerator();
        var nearVerts = new List<VertexId>();
        while (vertEnum2.MoveNext())
        {
            var v = network.GetVertex(vertEnum2.Current);
            if (Math.Abs(v.longitude - lon) < 0.002 && Math.Abs(v.latitude - lat) < 0.002)
                nearVerts.Add(vertEnum2.Current);
        }
        var shown = 0;
        foreach (var v in nearVerts)
        {
            edgeEnum2.MoveTo(v);
            while (edgeEnum2.MoveNext())
            {
                if (!edgeEnum2.Forward) continue;
                var fwd = costFunc.Get(edgeEnum2, true, null);
                var bwd = costFunc.Get(edgeEnum2, false, null);
                if (shown < 5 || (!fwd.canAccess && !bwd.canAccess))
                {
                    Console.WriteLine($"    Edge {edgeEnum2.EdgeId}: fwd(canAccess={fwd.canAccess}, canStop={fwd.canStop}, cost={fwd.cost:F1}) " +
                                      $"bwd(canAccess={bwd.canAccess}, canStop={bwd.canStop}, cost={bwd.cost:F1}) " +
                                      $"typeId={edgeEnum2.EdgeTypeId} length={edgeEnum2.Length}");
                    shown++;
                }
            }
        }

        Console.WriteLine($"\nSnap diagnostic for ({lon}, {lat}):");
        Console.WriteLine("=".PadRight(60, '='));

        // 1. Try snapping
        var snapResult = network.Snap(car).ToAsync(lon, lat).Result;
        Console.WriteLine($"Snap result: {(snapResult.IsError ? "FAILED - " + snapResult.ErrorMessage : "OK - EdgeId=" + snapResult.Value.EdgeId)}");

        // 2. Check what edges exist near the point
        var costFunction = network.GetCostFunctionFor(car);
        var edgeEnum = network.GetEdgeEnumerator();
        var vertEnum = network.GetVertexEnumerator();

        var searchRadius = 0.003; // ~300m
        var nearbyVertices = new List<VertexId>();

        while (vertEnum.MoveNext())
        {
            var v = network.GetVertex(vertEnum.Current);
            if (Math.Abs(v.longitude - lon) < searchRadius && Math.Abs(v.latitude - lat) < searchRadius)
            {
                nearbyVertices.Add(vertEnum.Current);
            }
        }

        Console.WriteLine($"\nVertices within {searchRadius} degrees: {nearbyVertices.Count}");

        // 3. For each nearby edge, check accessibility
        var accessible = 0;
        var inaccessible = 0;
        var total = 0;

        foreach (var vertex in nearbyVertices)
        {
            edgeEnum.MoveTo(vertex);
            while (edgeEnum.MoveNext())
            {
                if (!edgeEnum.Forward) continue; // avoid duplicates
                total++;

                var fwd = costFunction.Get(edgeEnum, true, null);
                var bwd = costFunction.Get(edgeEnum, false, null);

                var canAccess = fwd.canAccess || bwd.canAccess;

                if (canAccess)
                {
                    accessible++;
                }
                else
                {
                    inaccessible++;
                    // print first few inaccessible edges
                    if (inaccessible <= 3)
                    {
                        Console.WriteLine($"  Inaccessible: EdgeId={edgeEnum.EdgeId} " +
                                          $"Tail={edgeEnum.Tail} Head={edgeEnum.Head} " +
                                          $"EdgeTypeId={edgeEnum.EdgeTypeId} Length={edgeEnum.Length}");
                        foreach (var (key, value) in edgeEnum.Attributes)
                        {
                            Console.WriteLine($"    {key}={value}");
                        }
                    }
                }
            }
        }

        Console.WriteLine($"\nEdges: {total} total, {accessible} accessible, {inaccessible} inaccessible");

        // 4. Test round-trip: save to routerdb, reload, snap again
        Console.WriteLine("\nTesting routerdb round-trip...");
        var tmpPath = Path.GetTempFileName();
        using (var outStream = File.Create(tmpPath))
        {
            routerDb.WriteTo(outStream);
        }

        RouterDb reloaded;
        using (var inStream = File.OpenRead(tmpPath))
        {
            reloaded = RouterDb.ReadFrom(inStream);
        }
        File.Delete(tmpPath);

        reloaded.PrepareFor(car);
        var reloadedNetwork = reloaded.Latest;
        var reloadedSnap = reloadedNetwork.Snap(car).ToAsync(lon, lat).Result;
        Console.WriteLine($"After round-trip snap: {(reloadedSnap.IsError ? "FAILED - " + reloadedSnap.ErrorMessage : "OK - EdgeId=" + reloadedSnap.Value.EdgeId)}");

        // Check edge count near point after reload
        var reloadedEdgeEnum = reloadedNetwork.GetEdgeEnumerator();
        var reloadedVertEnum = reloadedNetwork.GetVertexEnumerator();
        var reloadedVertices = new List<VertexId>();
        while (reloadedVertEnum.MoveNext())
        {
            var v = reloadedNetwork.GetVertex(reloadedVertEnum.Current);
            if (Math.Abs(v.longitude - lon) < searchRadius && Math.Abs(v.latitude - lat) < searchRadius)
                reloadedVertices.Add(reloadedVertEnum.Current);
        }
        var reloadedEdgeCount = 0;
        foreach (var v in reloadedVertices)
        {
            reloadedEdgeEnum.MoveTo(v);
            while (reloadedEdgeEnum.MoveNext())
            {
                if (reloadedEdgeEnum.Forward) reloadedEdgeCount++;
            }
        }
        Console.WriteLine($"After round-trip: {reloadedVertices.Count} vertices, {reloadedEdgeCount} edges near point");

        // 5. Check if the tile at the location is loaded
        var tileX = (uint)((lon + 180.0) / 360.0 * (1 << 14));
        var tileY = (uint)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << 14));
        var tileId = (uint)(tileX + tileY * (1 << 14));
        Console.WriteLine($"\nTile at ({lon}, {lat}): x={tileX}, y={tileY}, id={tileId}");
        Console.WriteLine($"Tile loaded: {network.HasTile(tileId)}");
    }
}
