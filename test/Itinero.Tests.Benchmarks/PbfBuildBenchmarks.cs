using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Itinero.IO.Osm;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using OsmSharp.Streams;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// End-to-end PBF → RouterDb build benchmark. Measures the import pipeline
/// (OSM stream parse + tile + edge construction) that runs once per region
/// and dominates first-time setup.
///
/// Methods are split by region size into BDN categories so the default
/// invocation only runs the fast Luxembourg build:
///
/// <c>dotnet run -c Release --project test/Itinero.Tests.Benchmarks \
///     -- --filter "*PbfBuildBenchmarks*" --anyCategories=fast</c>
///
/// The Belgium variant is opt-in via <c>--anyCategories=slow</c>.
///
/// Iteration counts are intentionally low (PBF builds take many seconds);
/// BDN's default ~15 iterations would make the benchmark unusable.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 3)]
[BenchmarkCategory("pbf-build")]
public class PbfBuildBenchmarks
{
    private Profile _profile = null!;
    private string _luxembourgPath = null!;
    private string _belgiumPath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _profile = OsmProfiles.Car;
        _luxembourgPath = ResolvePath("luxembourg-latest.osm.pbf");
        _belgiumPath = ResolvePath("belgium-latest.osm.pbf");
    }

    [Benchmark]
    [BenchmarkCategory("fast")]
    public long BuildLuxembourg()
    {
        if (!File.Exists(_luxembourgPath))
        {
            throw new FileNotFoundException(
                $"Luxembourg PBF not found at '{_luxembourgPath}'. " +
                "Place a copy of luxembourg-latest.osm.pbf in the working directory " +
                "or under test/Itinero.Tests.Functional/ before running this benchmark.",
                _luxembourgPath);
        }
        return this.Build(_luxembourgPath);
    }

    [Benchmark]
    [BenchmarkCategory("slow")]
    public long BuildBelgium()
    {
        if (!File.Exists(_belgiumPath))
        {
            throw new FileNotFoundException(
                $"Belgium PBF not found at '{_belgiumPath}'. " +
                "Place a copy of belgium-latest.osm.pbf in the working directory " +
                "or under test/Itinero.Tests.Functional/ before running this benchmark.",
                _belgiumPath);
        }
        return this.Build(_belgiumPath);
    }

    private long Build(string pbfPath)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        routerDb.PrepareFor(_profile);

        using var osmStream = File.OpenRead(pbfPath);
        routerDb.UseOsmData(new PBFOsmStreamSource(osmStream));

        // Return a value derived from the built db so the JIT can't dead-code-
        // eliminate the build. The exact value doesn't matter; we just need
        // an observable side effect.
        var network = routerDb.Latest;
        long edgeCount = 0;
        var vertexEnumerator = network.GetVertexEnumerator();
        var edgeEnumerator = network.GetEdgeEnumerator();
        while (vertexEnumerator.MoveNext())
        {
            edgeEnumerator.MoveTo(vertexEnumerator.Current);
            while (edgeEnumerator.MoveNext()) edgeCount++;
        }
        return edgeCount;
    }

    private static string ResolvePath(string filename)
    {
        // BDN spawns benchmark workers in bin/Release/net10.0/<guid>/, so the
        // current directory isn't useful. Walk up from the assembly's location
        // until we find the repo's test data directory, then anchor there.
        if (File.Exists(filename)) return filename;

        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "test", "Itinero.Tests.Functional", filename);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }

        // Last-ditch: relative path. Caller will see the FileNotFoundException
        // and the message will at least show what we were looking for.
        return Path.Combine("test", "Itinero.Tests.Functional", filename);
    }
}
