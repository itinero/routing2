using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero.Tests.Benchmarks;

/// <summary>
/// End-to-end routing benchmarks measuring full Dijkstra performance
/// on a grid network. Routes from one corner to the opposite corner
/// to exercise a large portion of the graph.
/// </summary>
[MemoryDiagnoser]
public class RoutingBenchmarks
{
    private RoutingNetwork _network = null!;
    private Profile _profile = null!;
    private SnapPoint _source;
    private SnapPoint _target;

    [Params(50, 100)]
    public int GridSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _profile = OsmProfiles.Car;
        var routerDb = NetworkHelper.BuildGridNetwork(this.GridSize, this.GridSize, _profile);
        _network = routerDb.Latest;

        // snap to opposite corners of the grid.
        const double baseLon = 4.800;
        const double baseLat = 51.200;
        const double lonStep = 0.0013;
        const double latStep = 0.0009;

        var snapSource = _network.Snap(_profile).ToAsync(baseLon, baseLat).Result;
        var snapTarget = _network.Snap(_profile).ToAsync(
            baseLon + (this.GridSize - 1) * lonStep,
            baseLat + (this.GridSize - 1) * latStep).Result;

        _source = snapSource.Value;
        _target = snapTarget.Value;
    }

    [Benchmark]
    public async Task<bool> RouteCornerToCorner()
    {
        var route = await _network.Route(_profile)
            .From(_source)
            .To(_target)
            .CalculateAsync();

        return route.IsError;
    }
}
