using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// Routing benchmark with GC collection counts to trace allocation pressure.
/// Runs a single route and reports per-generation GC counts.
/// </summary>
public class AllocationTracingBenchmarks
{
    private RoutingNetwork _network = null!;
    private Profile _profile = null!;
    private SnapPoint _source;
    private SnapPoint _target;

    [GlobalSetup]
    public void Setup()
    {
        _profile = OsmProfiles.Car;
        var routerDb = NetworkHelper.BuildGridNetwork(100, 100, _profile);
        _network = routerDb.Latest;

        const double baseLon = 4.800;
        const double baseLat = 51.200;
        const double lonStep = 0.0013;
        const double latStep = 0.0009;

        var snapSource = _network.Snap(_profile).ToAsync(baseLon, baseLat).Result;
        var snapTarget = _network.Snap(_profile).ToAsync(
            baseLon + 99 * lonStep,
            baseLat + 99 * latStep).Result;

        _source = snapSource.Value;
        _target = snapTarget.Value;

        // warm up
        _network.Route(_profile).From(_source).To(_target).CalculateAsync().Wait();
    }

    [Benchmark]
    public void RouteWithGcCounts()
    {
        // force full collection before measuring.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        var allocBefore = GC.GetTotalAllocatedBytes(true);

        // run 10 routes to accumulate measurable numbers.
        for (var i = 0; i < 10; i++)
        {
            _network.Route(_profile).From(_source).To(_target).CalculateAsync().Wait();
        }

        var allocAfter = GC.GetTotalAllocatedBytes(true);
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);

        Console.WriteLine($"  [10 routes] Allocated: {(allocAfter - allocBefore) / 1024.0 / 1024.0:F2} MB, " +
                          $"Gen0: {gen0After - gen0Before}, Gen1: {gen1After - gen1Before}, Gen2: {gen2After - gen2Before}");
    }
}
