using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.TurnCosts;
using Itinero.Routes.Paths;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing.Flavours.Dijkstra;

public class DijkstraTurnCostTests
{
    [Fact]
    public async Task Dijkstra_OneToOne_TwoHopsShortest_WithoutTurnCost_ShouldFindTwoHopPath()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2, edge3;
        VertexId vertex1, vertex2, vertex3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            edge1 = mutable.AddEdge(vertex1, vertex2);
            edge2 = mutable.AddEdge(vertex2, vertex3);
            edge3 = mutable.AddEdge(vertex1, vertex3);
        }

        var latest = routerDb.Latest;
        var (path, cost) = await Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.RunAsync(latest,
            (await latest.Snap().ToAsync(vertex1).FirstAsync(), null),
            (await latest.Snap().ToAsync(vertex3).FirstAsync(), null),
            (e, ep) =>
            {
                var w = 1;
                if (e.EdgeId == edge3) w = 3;

                var tcs = e.GetTurnCostTo(ep)
                    .Select(x => (double)x.cost).Sum();
                return (w, tcs, false);
            });

        Assert.NotNull(path);
        path.Trim();
        Assert.Equal(0, path.Offset1);
        Assert.Equal(ushort.MaxValue, path.Offset2);
        using var enumerator = path.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(edge1, enumerator.Current.edge);
        Assert.True(enumerator.Current.forward);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(edge2, enumerator.Current.edge);
        Assert.True(enumerator.Current.forward);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public async Task Dijkstra_OneToOne_OneHopsShortest_OnlyWithTurnCost_ShouldFindOneHopPath()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2, edge3;
        VertexId vertex1, vertex2, vertex3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            edge1 = mutable.AddEdge(vertex1, vertex2);
            edge2 = mutable.AddEdge(vertex2, vertex3);
            edge3 = mutable.AddEdge(vertex1, vertex3);

            mutable.AddTurnCosts(vertex2, Enumerable.Empty<(string key, string value)>(),
                new[] { edge1, edge2 }, new uint[,] { { 0, 10 }, { 10, 0 } });
        }

        var latest = routerDb.Latest;
        var (path, cost) = await Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.RunAsync(latest,
            (await latest.Snap().ToAsync(vertex1).FirstAsync(), null),
            (await latest.Snap().ToAsync(vertex3).FirstAsync(), null),
            (e, ep) =>
            {
                var w = 1;
                if (e.EdgeId == edge3) w = 3;

                var tcs = e.GetTurnCostTo(ep)
                    .Select(x => (double)x.cost).Sum();
                return (w, tcs, false);
            });
        Assert.NotNull(path);
        path.Trim();
        Assert.Equal(0, path.Offset1);
        Assert.Equal(ushort.MaxValue, path.Offset2);
        using var enumerator = path.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(edge3, enumerator.Current.edge);
        Assert.True(enumerator.Current.forward);
    }

    [Fact]
    public async Task Dijkstra_OneToOne_TwoHopsShortest_InfiniteTurnCost_ShouldFindNoPath()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2;
        VertexId vertex1, vertex2, vertex3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085);

            edge1 = mutable.AddEdge(vertex1, vertex2);
            edge2 = mutable.AddEdge(vertex2, vertex3);

            mutable.AddTurnCosts(vertex2, Enumerable.Empty<(string key, string value)>(),
                new[] { edge1, edge2 }, new uint[,] { { 0, 1 }, { 0, 0 } });
        }

        var latest = routerDb.Latest;
        var (path, cost) = await Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.RunAsync(latest,
            (await latest.Snap().ToAsync(vertex1).FirstAsync(), null),
            (await latest.Snap().ToAsync(vertex3).FirstAsync(), null),
            (e, ep) =>
            {
                var tcs = e.GetTurnCostTo(ep)
                    .Select(x => (double)x.cost).Sum();
                if (tcs > 0) tcs = -1;
                return (1, tcs, false);
            });
        Assert.Null(path);
    }

    [Fact]
    public async Task Dijkstra_OneToOne_TwoHops_WithBarrier_ShouldNotFindPath()
    {
        var routerDb = new RouterDb();
        VertexId vertex1, vertex2, vertex3;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

            writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
        }

        var barriers = new HashSet<VertexId> { vertex2 };
        var latest = routerDb.Latest;
        var (path, _) = await Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.RunAsync(latest,
            (await latest.Snap().ToAsync(vertex1).FirstAsync(), null),
            (await latest.Snap().ToAsync(vertex3).FirstAsync(), null),
            (e, _) => (1, barriers.Contains(e.Tail) ? -1 : 0, false));

        Assert.Null(path);
    }

    [Fact]
    public async Task Dijkstra_OneToOne_FourHops_WithBarrier2_ShouldNotFindPath()
    {
        var routerDb = new RouterDb();
        VertexId vertex1, vertex2, vertex3, vertex4, vertex5;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex5 = writer.AddVertex(4.797506332397461, 51.26674845584085);

            writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
            writer.AddEdge(vertex3, vertex4);
            writer.AddEdge(vertex4, vertex5);
        }

        var barriers = new HashSet<VertexId> { vertex2 };
        var latest = routerDb.Latest;
        var (path, _) = await Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.RunAsync(latest,
            (await latest.Snap().ToAsync(vertex1).FirstAsync(), null),
            (await latest.Snap().ToAsync(vertex5).FirstAsync(), null),
            (e, _) => (1, barriers.Contains(e.Tail) ? -1 : 0, false));

        Assert.Null(path);
    }

    [Fact]
    public async Task Dijkstra_OneToOne_FourHops_WithBarrier3_ShouldNotFindPath()
    {
        var routerDb = new RouterDb();
        VertexId vertex1, vertex2, vertex3, vertex4, vertex5;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex5 = writer.AddVertex(4.797506332397461, 51.26674845584085);

            writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
            writer.AddEdge(vertex3, vertex4);
            writer.AddEdge(vertex4, vertex5);
        }

        var barriers = new HashSet<VertexId> { vertex3 };
        var latest = routerDb.Latest;
        var (path, _) = await Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.RunAsync(latest,
            (await latest.Snap().ToAsync(vertex1).FirstAsync(), null),
            (await latest.Snap().ToAsync(vertex5).FirstAsync(), null),
            (e, _) => (1, barriers.Contains(e.Tail) ? -1 : 0, false));

        Assert.Null(path);
    }

    [Fact]
    public async Task Dijkstra_OneToOne_FourHops_WithBarrier4_ShouldNotFindPath()
    {
        var routerDb = new RouterDb();
        VertexId vertex1, vertex2, vertex3, vertex4, vertex5;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex5 = writer.AddVertex(4.797506332397461, 51.26674845584085);

            writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
            writer.AddEdge(vertex3, vertex4);
            writer.AddEdge(vertex4, vertex5);
        }

        var barriers = new HashSet<VertexId> { vertex2 };
        var latest = routerDb.Latest;
        var (path, _) = await Itinero.Routing.Flavours.Dijkstra.Dijkstra.Default.RunAsync(latest,
            (await latest.Snap().ToAsync(vertex1).FirstAsync(), null),
            (await latest.Snap().ToAsync(vertex4).FirstAsync(), null),
            (e, _) => (1, barriers.Contains(e.Tail) ? -1 : 0, false));

        Assert.Null(path);
    }
}

