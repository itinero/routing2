using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.MapMatching;
using Itinero.MapMatching.Model;
using Itinero.Network;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.MapMatching;

public class ModelBuilderTests
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

    private static List<(int id, GraphNode node)> GetNodesForTrackPoint(GraphModel model, int trackPointIndex)
    {
        var result = new List<(int, GraphNode)>();
        for (var i = 0; i < model.Count; i++)
        {
            var node = model.GetNode(i);
            if (node.TrackPoint == trackPointIndex)
                result.Add((i, node));
        }
        return result;
    }

    [Fact]
    public async Task ModelBuilder_OneHopTrack()
    {
        var (network, _) = CreateNetwork(
            new[] { (4.784586882060637, 51.27024704344623), (4.785115480803853, 51.27048872037136) },
            new[] { (0, 1) });

        var track = new Track(new[]
        {
            new TrackPoint(4.784586882060637, 51.27024704344623),
            new TrackPoint(4.785115480803853, 51.27048872037136)
        });

        var modelBuilder = new ModelBuilder(network);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        Assert.Single(models);
        // should have start node + candidates for 2 points + end node
        Assert.True(models[0].Count >= 4);
    }

    [Fact]
    public async Task ModelBuilder_EmissionCostIsGaussian()
    {
        // A point closer to the road should have lower emission cost.
        // Road: east-west at lat 51.0 (~140m long)
        // Close point: ~5.5m north of road
        // Far point: ~22m north of road
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.002, 51.0) },
            new[] { (0, 1) });

        var track = new Track(new[]
        {
            new TrackPoint(4.001, 51.00005),   // ~5.5m north
            new TrackPoint(4.0015, 51.0002)    // ~22m north
        });

        var settings = new ModelBuilderSettings() { SigmaZ = 10.0, SearchRadius = 50 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        Assert.Single(models);
        var model = models[0];

        var closeNodes = GetNodesForTrackPoint(model, 0);
        var farNodes = GetNodesForTrackPoint(model, 1);

        Assert.NotEmpty(closeNodes);
        Assert.NotEmpty(farNodes);

        // Closer point should have lower emission cost (Gaussian: d^2 / 2*sigma_z^2)
        var minCloseCost = closeNodes.Min(n => n.node.Cost);
        var minFarCost = farNodes.Min(n => n.node.Cost);
        Assert.True(minCloseCost < minFarCost,
            $"Close point cost ({minCloseCost}) should be less than far point cost ({minFarCost})");

        // Verify quadratic relationship: cost ratio should reflect distance^2 ratio.
        // ~5.5m vs ~22m → cost ratio ≈ (22/5.5)^2 = 16
        var costRatio = minFarCost / minCloseCost;
        Assert.True(costRatio > 5,
            $"Cost ratio ({costRatio}) should reflect quadratic distance relationship");
    }

    [Fact]
    public async Task ModelBuilder_TransitionCostOnLShapedRoad()
    {
        // L-shaped road: route distance > great-circle distance, so transition cost > 0.
        // v1 -> v2 (east, ~70m) -> v3 (north, ~55m)
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.001, 51.0), (4.001, 51.0005) },
            new[] { (0, 1), (1, 2) });

        var track = new Track(new[]
        {
            new TrackPoint(4.0, 51.00002),     // near v1
            new TrackPoint(4.001, 51.00048)    // near v3
        });

        var settings = new ModelBuilderSettings() { SigmaZ = 10.0, Beta = 5.0, SearchRadius = 50 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        Assert.Single(models);
        var model = models[0];

        var layer1Nodes = GetNodesForTrackPoint(model, 0);
        var layer2Nodes = GetNodesForTrackPoint(model, 1);
        Assert.NotEmpty(layer1Nodes);
        Assert.NotEmpty(layer2Nodes);

        // On an L-shaped road, route distance > great-circle distance,
        // so |gc - route| > 0, and transition cost should be non-trivial.
        var hasNonZeroTransition = false;
        foreach (var (nodeId, _) in layer1Nodes)
        {
            foreach (var edge in model.GetNeighbours(nodeId))
            {
                if (layer2Nodes.Any(n => n.id == edge.Node2) && edge.Cost > 0.1)
                {
                    hasNonZeroTransition = true;
                }
            }
        }

        Assert.True(hasNonZeroTransition,
            "L-shaped road should produce non-zero transition cost (route distance > great-circle distance)");
    }

    [Fact]
    public async Task ModelBuilder_TransitionCostNearZeroOnStraightRoad()
    {
        // Straight road: route distance ≈ great-circle distance, so transition cost ≈ 0.
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.002, 51.0) },
            new[] { (0, 1) });

        var track = new Track(new[]
        {
            new TrackPoint(4.0005, 51.00002),  // near start of road
            new TrackPoint(4.0015, 51.00002)   // near end of road
        });

        var settings = new ModelBuilderSettings() { SigmaZ = 10.0, Beta = 5.0, SearchRadius = 50 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        Assert.Single(models);
        var model = models[0];

        var layer1Nodes = GetNodesForTrackPoint(model, 0);
        var layer2Nodes = GetNodesForTrackPoint(model, 1);
        Assert.NotEmpty(layer1Nodes);
        Assert.NotEmpty(layer2Nodes);

        // On a straight road, all transition costs should be near zero.
        foreach (var (nodeId, _) in layer1Nodes)
        {
            foreach (var edge in model.GetNeighbours(nodeId))
            {
                if (layer2Nodes.Any(n => n.id == edge.Node2))
                {
                    Assert.True(edge.Cost < 1.0,
                        $"Straight road transition cost ({edge.Cost}) should be near zero");
                }
            }
        }
    }

    [Fact]
    public async Task ModelBuilder_ClosePointsAreFiltered()
    {
        // Points closer than MinPointDistance should be skipped.
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.002, 51.0) },
            new[] { (0, 1) });

        // Point 0 and 1 are ~3.5m apart (< 10m MinPointDistance), point 2 is ~35m away
        var track = new Track(new[]
        {
            new TrackPoint(4.0005, 51.00002),    // point 0
            new TrackPoint(4.00055, 51.00002),   // point 1: ~3.5m east (should be filtered)
            new TrackPoint(4.001, 51.00002)      // point 2: ~35m east
        });

        var settings = new ModelBuilderSettings() { MinPointDistance = 10, SearchRadius = 50 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        Assert.Single(models);
        var model = models[0];

        // Point 1 should have been filtered (no nodes with TrackPoint == 1)
        var point1Nodes = GetNodesForTrackPoint(model, 1);
        Assert.Empty(point1Nodes);

        // Points 0 and 2 should have candidates
        Assert.NotEmpty(GetNodesForTrackPoint(model, 0));
        Assert.NotEmpty(GetNodesForTrackPoint(model, 2));
    }

    [Fact]
    public async Task ModelBuilder_SkipsOutlierPoints()
    {
        // A point far from any road should be skipped without breaking the model.
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.002, 51.0) },
            new[] { (0, 1) });

        var track = new Track(new[]
        {
            new TrackPoint(4.0005, 51.00002),   // point 0: near road (~2m)
            new TrackPoint(4.001, 51.001),       // point 1: ~111m from road (beyond 50m radius)
            new TrackPoint(4.0015, 51.00002)    // point 2: near road (~2m)
        });

        var settings = new ModelBuilderSettings() { SearchRadius = 50, MaxPointSkip = 3 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        // Should produce a single model (not broken by the outlier)
        Assert.Single(models);
        var model = models[0];

        // Point 1 should not have candidates (too far from road)
        Assert.Empty(GetNodesForTrackPoint(model, 1));

        // Points 0 and 2 should have candidates
        Assert.NotEmpty(GetNodesForTrackPoint(model, 0));
        Assert.NotEmpty(GetNodesForTrackPoint(model, 2));
    }

    [Fact]
    public async Task ModelBuilder_BreakageDistance()
    {
        // Points > BreakageDistance apart should produce separate models.
        // Two road segments ~2.1km apart.
        var (network, _) = CreateNetwork(
            new[]
            {
                (4.0, 51.0), (4.001, 51.0),    // Road A
                (4.03, 51.0), (4.031, 51.0)    // Road B (~2.1km east)
            },
            new[] { (0, 1), (2, 3) });

        var track = new Track(new[]
        {
            new TrackPoint(4.0005, 51.00002),   // near Road A
            new TrackPoint(4.0305, 51.00002)    // near Road B
        });

        var settings = new ModelBuilderSettings() { BreakageDistance = 2000, SearchRadius = 50 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        // No single model should contain candidates from both points.
        foreach (var model in models)
        {
            var p0Nodes = GetNodesForTrackPoint(model, 0);
            var p1Nodes = GetNodesForTrackPoint(model, 1);
            Assert.False(p0Nodes.Count > 0 && p1Nodes.Count > 0,
                "Breakage distance should prevent a single model from spanning both points");
        }
    }

    [Fact]
    public async Task ModelBuilder_SearchRadiusRespected()
    {
        // A point beyond the search radius should have no candidates.
        var (network, _) = CreateNetwork(
            new[] { (4.0, 51.0), (4.002, 51.0) },
            new[] { (0, 1) });

        var track = new Track(new[]
        {
            new TrackPoint(4.001, 51.00002),   // ~2m from road (within 50m)
            new TrackPoint(4.001, 51.001)      // ~111m from road (beyond 50m)
        });

        var settings = new ModelBuilderSettings() { SearchRadius = 50 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        // The far point should have no candidates in any model
        foreach (var model in models)
        {
            var farNodes = GetNodesForTrackPoint(model, 1);
            Assert.Empty(farNodes);
        }
    }

    [Fact]
    public async Task ModelBuilder_ParallelRoadsPreferNearer()
    {
        // Two parallel roads; track is much closer to Road A.
        // Road A at lat 51.0, Road B at lat 51.0003 (~33m north).
        // Track at lat 51.00005 (~5.5m north of Road A, ~27.8m from Road B).
        var (network, edgeIds) = CreateNetwork(
            new[]
            {
                (4.0, 51.0), (4.002, 51.0),         // Road A
                (4.0, 51.0003), (4.002, 51.0003)    // Road B
            },
            new[] { (0, 1), (2, 3) });

        var edgeA = edgeIds[0];
        var edgeB = edgeIds[1];

        var track = new Track(new[]
        {
            new TrackPoint(4.0005, 51.00005),
            new TrackPoint(4.001, 51.00005),
            new TrackPoint(4.0015, 51.00005)
        });

        var settings = new ModelBuilderSettings() { SigmaZ = 10.0, SearchRadius = 50 };
        var modelBuilder = new ModelBuilder(network, settings);
        var models = (await modelBuilder.BuildModels(track, new DefaultProfile())).ToList();

        Assert.Single(models);
        var model = models[0];

        // For each track point, the lowest-cost candidate should be on Road A (closer road).
        for (var tp = 0; tp < 3; tp++)
        {
            var nodes = GetNodesForTrackPoint(model, tp);
            Assert.NotEmpty(nodes);

            var bestNode = nodes.OrderBy(n => n.node.Cost).First();
            Assert.NotNull(bestNode.node.SnapPoint);
            var bestEdge = bestNode.node.SnapPoint!.Value.EdgeId;
            Assert.True(bestEdge.TileId == edgeA.TileId && bestEdge.LocalId == edgeA.LocalId,
                $"Track point {tp}: best candidate should be on Road A (nearer), " +
                $"got edge ({bestEdge.TileId},{bestEdge.LocalId}) vs expected ({edgeA.TileId},{edgeA.LocalId})");
        }
    }
}
