using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// Standalone allocation profiler that can be run via:
///   dotnet run --project test/Itinero.Tests.Benchmarks -c Release -- profile-allocs
/// </summary>
internal static class AllocationProfiler
{
    internal static void Run()
    {
        Console.WriteLine("Building 100x100 grid network...");
        var profile = OsmProfiles.Car;
        var routerDb = NetworkHelper.BuildGridNetwork(100, 100, profile);
        var network = routerDb.Latest;

        const double baseLon = 4.800;
        const double baseLat = 51.200;
        const double lonStep = 0.0013;
        const double latStep = 0.0009;

        var source = network.Snap(profile).ToAsync(baseLon, baseLat).Result.Value;
        var target = network.Snap(profile).ToAsync(
            baseLon + 99 * lonStep, baseLat + 99 * latStep).Result.Value;

        // warm up.
        network.Route(profile).From(source).To(target).CalculateAsync().Wait();

        Console.WriteLine("Profiling 10 routes...");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocBefore = GC.GetTotalAllocatedBytes(true);
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        const int routeCount = 10;
        for (var i = 0; i < routeCount; i++)
        {
            network.Route(profile).From(source).To(target).CalculateAsync().Wait();
        }

        var allocAfter = GC.GetTotalAllocatedBytes(true);
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);

        var totalAlloc = allocAfter - allocBefore;
        var perRoute = totalAlloc / routeCount;

        Console.WriteLine();
        Console.WriteLine($"Total allocated:     {totalAlloc / 1024.0 / 1024.0:F2} MB ({routeCount} routes)");
        Console.WriteLine($"Per route:           {perRoute / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"GC Gen0 collections: {gen0After - gen0Before}");
        Console.WriteLine($"GC Gen1 collections: {gen1After - gen1Before}");
        Console.WriteLine($"GC Gen2 collections: {gen2After - gen2Before}");

        // Now do a detailed breakdown by taking a snapshot approach:
        // Run one route while tracking allocations via the GC callbacks.
        Console.WriteLine();
        Console.WriteLine("Detailed per-phase breakdown (1 route):");
        Console.WriteLine("========================================");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var phases = new List<(string name, long bytes)>();

        var before = GC.GetTotalAllocatedBytes(true);

        // Phase 1: Build route request
        var routeBuilder = network.Route(profile).From(source).To(target);
        phases.Add(("Route builder setup", Measure(ref before)));

        // Phase 2: Calculate
        var route = routeBuilder.CalculateAsync().Result;
        phases.Add(("Dijkstra + path construction", Measure(ref before)));

        // Phase 3: Access result
        var shape = route.Value;
        phases.Add(("Result access", Measure(ref before)));

        Console.WriteLine();
        long total = 0;
        foreach (var (name, bytes) in phases)
        {
            Console.WriteLine($"  {name,-35} {bytes / 1024.0:F1} KB");
            total += bytes;
        }
        Console.WriteLine($"  {"TOTAL",-35} {total / 1024.0:F1} KB");

        // Data structure sizing analysis.
        Console.WriteLine();
        Console.WriteLine("Data structure sizing (100x100 grid, corner-to-corner):");
        Console.WriteLine("======================================================");

        // The grid has 100*100 = 10,000 vertices.
        // Corner-to-corner visits most of them.
        // PathTree stores 7 uint per visit = 28 bytes/visit.
        // BinaryHeap stores 1 uint + 1 double per entry = 12 bytes/entry.
        var vertices = 100 * 100;
        var edgesPerVertex = 4; // grid interior
        var totalEdges = vertices * edgesPerVertex; // each edge visited twice
        Console.WriteLine($"  Grid vertices:     {vertices}");
        Console.WriteLine($"  Approx edge evals: {totalEdges}");
        Console.WriteLine($"  PathTree (7 uint/visit × {vertices} visits): {vertices * 7 * 4 / 1024.0:F0} KB");
        Console.WriteLine($"  BinaryHeap (12 bytes × {totalEdges} pushes): {totalEdges * 12 / 1024.0:F0} KB");
        Console.WriteLine($"  HashSet<VertexId> ({vertices} entries × ~24 bytes): {vertices * 24 / 1024.0:F0} KB");
        Console.WriteLine();
        Console.WriteLine("  Array.Resize doubling creates throwaway copies.");
        Console.WriteLine($"  PathTree doubles: ~{EstimateResizeCost(1024, (uint)(vertices * 7)) / 1024.0:F0} KB in discarded arrays");
        Console.WriteLine($"  BinaryHeap doubling resizes: ~{EstimateResizeCost(1024, (uint)totalEdges) * 12 / 4 / 1024.0:F0} KB in discarded arrays (12 bytes/entry)");

        // Micro-phase breakdown: measure individual operations.
        Console.WriteLine();
        Console.WriteLine("Micro-phase: individual operation costs:");
        Console.WriteLine("========================================");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();

        // Measure cost of just enumerating all edges (no routing).
        var bEdges = GC.GetTotalAllocatedBytes(true);
        var edgeEnum = network.GetEdgeEnumerator();
        var vertEnum = network.GetVertexEnumerator();
        var edgeCount = 0;
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext()) edgeCount++;
        }
        var aEdges = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  Edge enumeration ({edgeCount} edges):   {(aEdges - bEdges) / 1024.0:F1} KB");

        // Measure cost of HashSet operations.
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var bHash = GC.GetTotalAllocatedBytes(true);
        var hashSet = new System.Collections.Generic.HashSet<Itinero.Network.VertexId>();
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext()) hashSet.Add(vertEnum.Current);
        var aHash = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  HashSet<VertexId> ({hashSet.Count} adds): {(aHash - bHash) / 1024.0:F1} KB");

        // Measure cost of Dijkstra using sync approach to isolate async overhead.
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var bSync = GC.GetTotalAllocatedBytes(true);
        network.Route(profile).From(source).To(target).CalculateAsync().Wait();
        var aSync = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  Full route (async .Wait()):    {(aSync - bSync) / 1024.0:F1} KB");

        // Measure cost function in isolation.
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var costFunc = network.GetCostFunctionFor(profile);
        edgeEnum = network.GetEdgeEnumerator();

        // Warm up cost function cache.
        vertEnum = network.GetVertexEnumerator();
        vertEnum.MoveNext();
        edgeEnum.MoveTo(vertEnum.Current);
        edgeEnum.MoveNext();
        costFunc.Get(edgeEnum, true, null);

        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var bCost = GC.GetTotalAllocatedBytes(true);
        var costCount = 0;
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                costFunc.Get(edgeEnum, true, null);
                costCount++;
            }
        }
        var aCost = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  Cost function ({costCount} calls):  {(aCost - bCost) / 1024.0:F1} KB ({(aCost - bCost) / costCount} bytes/call)");

        // Break down what the cost function does internally.
        Console.WriteLine();
        Console.WriteLine("Cost function breakdown (per 39600 calls):");
        Console.WriteLine("==========================================");

        // 1. Just ArraySegment.Empty.FirstOrDefault()
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var b1 = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < costCount; i++)
        {
            IEnumerable<(Itinero.Network.EdgeId edgeId, byte? turn)>? prev = null;
            prev ??= ArraySegment<(Itinero.Network.EdgeId edgeId, byte? turn)>.Empty;
            var (_, turn) = prev.FirstOrDefault();
        }
        var a1 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  ArraySegment.Empty.FirstOrDefault(): {(a1 - b1) / 1024.0:F1} KB ({(a1 - b1) / costCount} bytes/call)");

        // 2. Just EdgeTypeId + cache lookup
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var b2 = GC.GetTotalAllocatedBytes(true);
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                _ = edgeEnum.EdgeTypeId;
                _ = edgeEnum.Length;
                _ = edgeEnum.Forward;
            }
        }
        var a2 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  EdgeTypeId+Length+Forward reads:      {(a2 - b2) / 1024.0:F1} KB ({(a2 - b2) / costCount} bytes/call)");

        // 3. costFunc.Get with empty ArraySegment (no null)
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var emptyPrev = (IEnumerable<(Itinero.Network.EdgeId edgeId, byte? turn)>)ArraySegment<(Itinero.Network.EdgeId edgeId, byte? turn)>.Empty;
        var b3 = GC.GetTotalAllocatedBytes(true);
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                costFunc.Get(edgeEnum, true, emptyPrev);
            }
        }
        var a3 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  costFunc.Get(pre-boxed empty):       {(a3 - b3) / 1024.0:F1} KB ({(a3 - b3) / costCount} bytes/call)");

        // 4. Inline what the cost function does - no interface, no LINQ.
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var b4 = GC.GetTotalAllocatedBytes(true);
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                var eti = edgeEnum.EdgeTypeId;
                var len = edgeEnum.Length;
                var fwd = edgeEnum.Forward;
                // just read the values, don't call cost function
            }
        }
        var a4 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  Manual prop reads (no costFunc):     {(a4 - b4) / 1024.0:F1} KB ({(a4 - b4) / costCount} bytes/call)");

        // 5. Just FirstOrDefault on pre-boxed empty per call
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var b5 = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < costCount; i++)
        {
            var (_, turn5) = emptyPrev.FirstOrDefault();
        }
        var a5 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  FirstOrDefault on pre-boxed empty:   {(a5 - b5) / 1024.0:F1} KB ({(a5 - b5) / costCount} bytes/call)");

        // 6. costFunc.Get passing Array.Empty (true empty, no elements)
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var trueEmpty = Array.Empty<(Itinero.Network.EdgeId edgeId, byte? turn)>();
        var b6 = GC.GetTotalAllocatedBytes(true);
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                costFunc.Get(edgeEnum, true, trueEmpty);
            }
        }
        var a6 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  costFunc.Get(Array.Empty):           {(a6 - b6) / 1024.0:F1} KB ({(a6 - b6) / costCount} bytes/call)");

        // 7. Check if Length is null (triggers EdgeLength which allocates)
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var nullLengthCount = 0;
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                if (edgeEnum.Length == null) nullLengthCount++;
            }
        }
        Console.WriteLine($"  Edges with null Length:               {nullLengthCount} / {costCount}");

        // 8. Check EdgeTypeId nulls
        var nullTypeCount = 0;
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                if (edgeEnum.EdgeTypeId == null) nullTypeCount++;
            }
        }
        Console.WriteLine($"  Edges with null EdgeTypeId:           {nullTypeCount} / {costCount}");

        // 9. Measure just Attributes access
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var b9 = GC.GetTotalAllocatedBytes(true);
        vertEnum = network.GetVertexEnumerator();
        while (vertEnum.MoveNext())
        {
            edgeEnum.MoveTo(vertEnum.Current);
            while (edgeEnum.MoveNext())
            {
                foreach (var _ in edgeEnum.Attributes) { }
            }
        }
        var a9 = GC.GetTotalAllocatedBytes(true);
        Console.WriteLine($"  Attributes enumeration:              {(a9 - b9) / 1024.0:F1} KB ({(a9 - b9) / costCount} bytes/call)");
    }

    private static long EstimateResizeCost(uint initialSize, uint finalSize)
    {
        long total = 0;
        var size = initialSize;
        while (size < finalSize)
        {
            total += size * 4; // old array becomes garbage.
            size *= 2;
        }
        return total;
    }

    private static long EstimateHeapResizeCost(uint initialSize, uint finalSize)
    {
        long total = 0;
        var size = initialSize;
        while (size < finalSize)
        {
            total += size * 12; // uint[] + double[] become garbage.
            size += 100;
        }
        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(ref long before)
    {
        var after = GC.GetTotalAllocatedBytes(true);
        var delta = after - before;
        before = after;
        return delta;
    }
}
