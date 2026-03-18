using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.MapMatching;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes.Paths;
using Xunit;

namespace Itinero.Tests.MapMatching;

public class MapMatcherTests
{
    private static (RoutingNetwork network, List<EdgeId> edgeIds) CreateNetwork(
        (double lon, double lat)[] vertices, (int from, int to)[] edges)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration() { MaxIslandSize = 0 });
        var writer = routerDb.Latest.GetWriter();
        var vertexIds = new List<VertexId>();
        foreach (var (lon, lat) in vertices)
        {
            vertexIds.Add(writer.AddVertex(lon, lat));
        }
        var edgeIds = new List<EdgeId>();
        foreach (var (from, to) in edges)
        {
            edgeIds.Add(writer.AddEdge(vertexIds[from], vertexIds[to], null, null, null, writer.ComputeEdgeLength(vertexIds[from], vertexIds[to])));
        }
        writer.Dispose();
        return (routerDb.Latest, edgeIds);
    }

    [Fact]
    public async Task MapMatcher_StraightRoadMatch()
    {
        // Simple straight road, track points along it.
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.002, 51.0) },
            new[] { (0, 1) });

        var track = new Track(new[]
        {
            new TrackPoint(4.0005, 51.00002),
            new TrackPoint(4.001, 51.00002),
            new TrackPoint(4.0015, 51.00002)
        });

        var matcher = network.Matcher(s =>
        {
            s.Profile = new DefaultProfile();
            s.SearchRadius = 50;
        });

        var matches = (await matcher.MatchAsync(track)).ToList();

        Assert.NotEmpty(matches);
        Assert.True(matches.Any(m => m.Count > 0), "Match should contain at least one path");
    }

    [Fact]
    public async Task MapMatcher_MultiEdgeRoadProducesReasonablePathLength()
    {
        // Three vertices, two edges, track spans the full road.
        // Road total length ~140m (two ~70m edges).
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.001, 51.0), (4.002, 51.0) },
            new[] { (0, 1), (1, 2) });

        var track = new Track(new[]
        {
            new TrackPoint(4.0002, 51.00002),
            new TrackPoint(4.001, 51.00002),
            new TrackPoint(4.0018, 51.00002)
        });

        var matcher = network.Matcher(s =>
        {
            s.Profile = new DefaultProfile();
            s.SearchRadius = 50;
        });

        var matches = (await matcher.MatchAsync(track)).ToList();
        Assert.NotEmpty(matches);

        var totalLength = 0.0;
        foreach (var match in matches)
        {
            for (var i = 0; i < match.Count; i++)
            {
                totalLength += match[i].LengthInMeters();
            }
        }

        // Track spans most of the ~140m road
        Assert.True(totalLength > 50, $"Total matched path length ({totalLength}m) should be > 50m");
        Assert.True(totalLength < 300, $"Total matched path length ({totalLength}m) should be < 300m");
    }

    [Fact]
    public async Task MapMatcher_ParallelRoadsMatchesNearer()
    {
        // Two parallel roads; track is much closer to Road A.
        // Road A at lat 51.0, Road B at lat 51.0003 (~33m north).
        // Track at lat 51.00005 (~5.5m north of Road A).
        var (network, edgeIds) = CreateNetwork(
            new[]
            {
                (4.0, 51.0), (4.002, 51.0),         // Road A
                (4.0, 51.0003), (4.002, 51.0003)    // Road B
            },
            new[] { (0, 1), (2, 3) });

        var edgeA = edgeIds[0];

        var track = new Track(new[]
        {
            new TrackPoint(4.0005, 51.00005),
            new TrackPoint(4.001, 51.00005),
            new TrackPoint(4.0015, 51.00005)
        });

        var matcher = network.Matcher(s =>
        {
            s.Profile = new DefaultProfile();
            s.SigmaZ = 10.0;
            s.SearchRadius = 50;
        });

        var matches = (await matcher.MatchAsync(track)).ToList();
        Assert.NotEmpty(matches);

        // All matched path edges should be on Road A (the nearer road).
        foreach (var match in matches)
        {
            for (var i = 0; i < match.Count; i++)
            {
                var path = match[i];
                foreach (var (edge, _, _, _) in path)
                {
                    Assert.True(edge.TileId == edgeA.TileId && edge.LocalId == edgeA.LocalId,
                        $"Matched edge ({edge.TileId},{edge.LocalId}) should be Road A ({edgeA.TileId},{edgeA.LocalId})");
                }
            }
        }
    }
}
