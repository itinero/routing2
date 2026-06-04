using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra.Bidirectional;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing.Flavours.Dijkstra.Bidirectional;

/// <summary>
/// Basic correctness tests for the new edge-based bidirectional Dijkstra. Covers the
/// classic (non-access-aware) cases first; the access-aware path-shape suite lives in
/// <see cref="BidirectionalDijkstraAccessAwareTests"/>.
/// </summary>
public class BidirectionalDijkstraTests
{
    [Fact]
    public async Task OneHop_FindsPath()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId v1, v2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            v2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(v1, v2);
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var source = await network.Snap().ToAsync(v1).FirstAsync();
        var target = await network.Snap().ToAsync(v2).FirstAsync();

        var (path, _) = await BidirectionalDijkstra.Default.RunAsync(
            network, source, target, network.GetCostFunctionFor(profile));

        Assert.NotNull(path);
        Assert.Single(path);
        Assert.Equal(edge, path[0].edge);
    }

    [Fact]
    public async Task TwoHops_FindsPath()
    {
        var routerDb = new RouterDb();
        var edges = new List<EdgeId>();
        VertexId v1, v3;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            v3 = writer.AddVertex(4.796, 51.267);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var source = await network.Snap().ToAsync(v1).FirstAsync();
        var target = await network.Snap().ToAsync(v3).FirstAsync();

        var (path, _) = await BidirectionalDijkstra.Default.RunAsync(
            network, source, target, network.GetCostFunctionFor(profile));

        Assert.NotNull(path);
        Assert.Equal(2, path.Count);
        Assert.Equal(edges[0], path[0].edge);
        Assert.Equal(edges[1], path[1].edge);
    }

    [Fact]
    public async Task DisconnectedComponents_ReturnsNull()
    {
        var routerDb = new RouterDb();
        VertexId v1, v3;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            v3 = writer.AddVertex(4.7, 51.2);
            var v4 = writer.AddVertex(4.71, 51.21);
            writer.AddEdge(v1, v2);
            writer.AddEdge(v3, v4);
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var source = await network.Snap().ToAsync(v1).FirstAsync();
        var target = await network.Snap().ToAsync(v3).FirstAsync();

        var (path, cost) = await BidirectionalDijkstra.Default.RunAsync(
            network, source, target, network.GetCostFunctionFor(profile));

        Assert.Null(path);
        Assert.Equal(double.MaxValue, cost);
    }

    [Fact]
    public async Task TwoHops_MidEdgeOffsets_FindsPath()
    {
        // Snap points in the middle of source and target edges. Exercises the offset-aware
        // initial pushes in both halves and the meeting at the shared vertex.
        var routerDb = new RouterDb();
        var edges = new List<EdgeId>();
        VertexId v1, v3;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v1 = writer.AddVertex(4.792, 51.265);
            var v2 = writer.AddVertex(4.794, 51.266);
            v3 = writer.AddVertex(4.796, 51.267);
            edges.Add(writer.AddEdge(v1, v2));
            edges.Add(writer.AddEdge(v2, v3));
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var source = new SnapPoint(edges[0], ushort.MaxValue / 2);
        var target = new SnapPoint(edges[1], ushort.MaxValue / 2);

        var (path, _) = await BidirectionalDijkstra.Default.RunAsync(
            network, source, target, network.GetCostFunctionFor(profile));

        Assert.NotNull(path);
        Assert.Equal(2, path.Count);
        Assert.Equal(edges[0], path[0].edge);
        Assert.Equal(edges[1], path[1].edge);
    }

    [Fact]
    public async Task TwoEdges_WithShapePoints_FindsPath()
    {
        // Direct repro of IRouterOneToOneExtensions_Calculate_TwoEdge_With2ShapePoints_ShouldMatchEdges.
        var (routerDb, _, edges) = RouterDbScaffolding.BuildRouterDb(new (double longitude, double latitude, float? e)[] {
                (4.801073670387268, 51.268064181900094, (float?)null),
                (4.801771044731140, 51.268886491558250, (float?)null),
                (4.802438914775848, 51.268097745847655, (float?)null),
            },
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                (0, 1, new (double longitude, double latitude, float? e)[] {
                    (4.800950288772583, 51.268426671236426, (float?)null),
                    (4.801242649555205, 51.268816008449830, (float?)null),
                }),
                (1, 2, new (double longitude, double latitude, float? e)[] {
                    (4.802066087722777, 51.268582742153434, (float?)null),
                    (4.801921248435973, 51.268258852454680, (float?)null),
                }),
            });

        var network = routerDb.Latest;
        var snap1 = await network.Snap().ToAsync((4.801073670387268, 51.268064181900094, (float?)null));
        var snap2 = await network.Snap().ToAsync((4.802438914775848, 51.268097745847650, (float?)null));
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        var profile = new DefaultProfile();
        var (path, _) = await BidirectionalDijkstra.Default.RunAsync(
            network, snap1.Value, snap2.Value, network.GetCostFunctionFor(profile));

        Assert.NotNull(path);
    }

    [Fact]
    public async Task SameEdge_SameOffset_ReturnsZeroCost()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId v1, v2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            v1 = writer.AddVertex(4.792, 51.265);
            v2 = writer.AddVertex(4.797, 51.266);
            edge = writer.AddEdge(v1, v2);
        }

        var profile = new DefaultProfile();
        var network = routerDb.Latest;
        var sp = new SnapPoint(edge, ushort.MaxValue / 2);

        var (path, cost) = await BidirectionalDijkstra.Default.RunAsync(
            network, sp, sp, network.GetCostFunctionFor(profile));

        Assert.NotNull(path);
        Assert.Equal(0, cost);
    }
}
