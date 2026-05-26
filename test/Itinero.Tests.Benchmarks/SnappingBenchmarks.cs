using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Itinero.IO.Osm;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Snapping;
using OsmSharp.Streams;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// Snap-to-edge benchmarks against the real-world Belgium routing graph.
/// Measures the cost of finding the nearest edge from a lon/lat coordinate
/// — the path most users hit on every routing request.
///
/// Setup loads <c>belgium-latest.osm.routerdb</c> if present; otherwise it
/// builds from <c>belgium-latest.osm.pbf</c>, downloading the PBF first if
/// needed. The built RouterDb is cached to disk so subsequent benchmark
/// sessions skip the expensive build step. This mirrors
/// <c>test/Itinero.Tests.Functional/Program.cs</c>'s <c>GetOrCreate</c>.
///
/// Every location is warmed during <see cref="Setup"/> so the benchmark
/// methods measure steady-state snap performance against hot tiles, not
/// first-touch tile loading. That's the regime real services run in and the
/// regime any snap-algorithm refactor needs to win.
///
/// Job config: <c>[SimpleJob(warmupCount: 2, iterationCount: 4)]</c>
/// tightens the measurement loop from BDN's default ~15 iterations. CIs
/// widen (StdErr ~1-3% of mean instead of ~0.1%) but A/B comparisons for
/// a refactor stay valid. Out-of-process toolchain (one worker per case)
/// is the default and we keep it — an attempt at InProcess hit an
/// IndexOutOfRangeException deep in <c>NetworkTile.CloneForEdgeTypeMap</c>,
/// likely Itinero state being unsafely reused across 56 in-process setups.
///
/// Setup only warms <see cref="LocationIndex"/>'s tile — not all 28 — so
/// per-case setup stays cheap (one snap, ~ms-to-hundreds-of-ms depending
/// on density) instead of paying the full 28-location warm-up bill 56
/// times. Cross-case tile caching is sacrificed (each worker is fresh
/// anyway), but the targeted warm is what each case actually needs.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 4)]
public class SnappingBenchmarks
{
    private const string BelgiumPbfUrl =
        "http://planet.anyways.eu/planet/europe/belgium/belgium-latest.osm.pbf";

    /// <summary>
    /// All locations the benchmarks parametrize over. Index 0..7 are the
    /// 8 hand-picked named scenarios across Belgium (urban / suburb / village
    /// / rural / coast / offshore-miss / 2× motorway). Indices 8..27 are 20
    /// pre-computed Halton-distributed points inside the Flanders bbox
    /// (lon [3.244, 5.371], lat [50.771, 51.214]) — Halton with coprime bases
    /// 2 and 3 spreads points more evenly than uniform random at small N.
    /// The values are hardcoded so they cannot drift between runs or
    /// machines.
    ///
    /// Per-route real-world traffic is "snap two endpoints"; we measure each
    /// snap individually as a BDN case rather than aggregating, so a refactor
    /// that wins on average but regresses on a specific kind of location
    /// shows up in the table instead of being averaged away.
    /// </summary>
    private static readonly (string name, double lon, double lat)[] Locations =
    [
        ("urban-brussels",         4.354680, 50.846553),
        ("suburb-zellik",          4.273928, 50.884507),
        ("village-wechelderzande", 4.801290, 51.267740),
        ("rural-vorselaar",        4.766854, 51.237571),
        ("coast-brugge",           3.218422, 51.213369),
        ("offshore-miss",          3.000000, 51.500000),
        ("motorway-north",         4.997113875551122, 51.29585035231747),
        ("motorway-south",         5.088890015663452, 50.13035971543704),
        ("halton-00",              4.307845932016534, 50.91886660986037),
        ("halton-01",              3.776069042717694, 51.066473619850456),
        ("halton-02",              4.839622821315373, 50.820461936533654),
        ("halton-03",              3.510180598068274, 50.968068946523736),
        ("halton-04",              4.5737343766659535, 51.11567595651382),
        ("halton-05",              4.041957487367114, 50.86966427319701),
        ("halton-06",              5.105511265964793, 51.01727128318709),
        ("halton-07",              3.377236375743564, 51.164878293177175),
        ("halton-08",              4.440790154341244, 50.787660378758076),
        ("halton-09",              3.909013265042404, 50.93526738874816),
        ("halton-10",              4.972567043640083, 51.08287439873824),
        ("halton-11",              3.643124820392984, 50.83686271542144),
        ("halton-12",              4.706678598990663, 50.98446972541152),
        ("halton-13",              4.174901709691824, 51.132076735401604),
        ("halton-14",              5.238455488289503, 50.8860650520848),
        ("halton-15",              3.310764264581209, 51.033672062074885),
        ("halton-16",              4.374318043178889, 51.18127907206497),
        ("halton-17",              3.842541153880049, 50.80406115764586),
        ("halton-18",              4.906094932477728, 50.951668167635944),
        ("halton-19",              3.576652709230629, 51.099275177626026),
    ];

    private RoutingNetwork _network = null!;
    private Profile _profile = null!;

    [ParamsSource(nameof(LocationIndices))]
    public int LocationIndex { get; set; }

    public IEnumerable<int> LocationIndices => Enumerable.Range(0, Locations.Length);

    /// <summary>
    /// Snap settings chosen to match what publish-api actually configures in
    /// production — not the Itinero defaults. The defaults (100m / 500m / 100m)
    /// search a 0.04 km² box; publish-api opens it up to 100 km² and removes
    /// the distance cutoff entirely. That blows up the per-snap candidate
    /// count by ~100× in dense urban networks, which is exactly the regime
    /// any snap refactor needs to win in. Benchmarking with the defaults
    /// would understate the headline number by orders of magnitude.
    ///
    /// Settings table:
    ///   OffsetInMeter    5000  — initial search box radius
    ///   OffsetInMeterMax 5000  — retry box radius (same here; publish-api
    ///                            never grows the box because the first try
    ///                            already covers a huge area)
    ///   MaxDistance      ∞    — accept any snap, however far
    /// </summary>
    private static void ApplyRealWorldSnapSettings(SnapperSettings s)
    {
        s.OffsetInMeter = 5000;
        s.OffsetInMeterMax = 5000;
        s.MaxDistance = double.MaxValue;
    }

    [GlobalSetup]
    public void Setup()
    {
        _profile = OsmProfiles.Car;
        var routerDb = LoadOrBuild(_profile);
        _network = routerDb.Latest;

        // Each BDN case spawns its own worker process with a fresh RouterDb,
        // so cross-case tile caching is impossible. Warm only the one
        // location this case measures — keeps per-case setup proportional to
        // the cost of a single snap, not 28 of them.
        var loc = Locations[this.LocationIndex];
        this.WarmOne(loc.lon, loc.lat);
    }

    private void WarmOne(double lon, double lat)
    {
        _ = _network.Snap(_profile, ApplyRealWorldSnapSettings)
            .ToAsync(lon, lat).GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task<EdgeId> ToAsync_Single()
    {
        var loc = Locations[this.LocationIndex];
        var result = await _network.Snap(_profile, ApplyRealWorldSnapSettings)
            .ToAsync(loc.lon, loc.lat);
        return result.IsError ? EdgeId.Empty : result.Value.EdgeId;
    }

    [Benchmark]
    public async Task<int> ToAllAsync_Drain()
    {
        var loc = Locations[this.LocationIndex];
        var count = 0;
        await foreach (var snap in _network.Snap(_profile, ApplyRealWorldSnapSettings)
            .ToAllAsync(loc.lon, loc.lat))
        {
            _ = snap;
            count++;
        }
        return count;
    }

    private static RouterDb LoadOrBuild(Profile profile)
    {
        var routerDbPath = ResolvePath("belgium-latest.osm.routerdb");
        if (File.Exists(routerDbPath))
        {
            Console.WriteLine($"[SnappingBenchmarks] loading RouterDb from {routerDbPath}");
            using var stream = File.OpenRead(routerDbPath);
            var loaded = RouterDb.ReadFrom(stream);
            loaded.EdgeTypeMap = new OsmEdgeTypeMap();
            loaded.PrepareFor(profile);
            return loaded;
        }

        var pbfPath = ResolvePath("belgium-latest.osm.pbf");
        if (!File.Exists(pbfPath))
        {
            Console.WriteLine($"[SnappingBenchmarks] downloading Belgium PBF to {pbfPath}");
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            using var src = client.GetStreamAsync(BelgiumPbfUrl).GetAwaiter().GetResult();
            using var dst = File.Create(pbfPath);
            src.CopyTo(dst);
        }

        Console.WriteLine($"[SnappingBenchmarks] building RouterDb from {pbfPath} (slow, one-time)");
        var built = new RouterDb(new RouterDbConfiguration
        {
            Zoom = 14,
            EdgeTypeMap = new OsmEdgeTypeMap()
        });
        built.PrepareFor(profile);
        using (var osmStream = File.OpenRead(pbfPath))
        {
            built.UseOsmData(new PBFOsmStreamSource(osmStream));
        }

        Console.WriteLine($"[SnappingBenchmarks] saving RouterDb to {routerDbPath}");
        using (var outStream = File.Create(routerDbPath))
        {
            built.WriteTo(outStream);
        }

        return built;
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

        // Fall through with the relative path so the caller can produce a
        // useful FileNotFoundException message naming what we couldn't find.
        return Path.Combine("test", "Itinero.Tests.Functional", filename);
    }
}
