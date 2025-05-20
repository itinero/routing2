using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Routing.Flavours.Dijkstra.Bidirectional;
using Itinero.Tests.Mocks.Costs;
using Xunit;

namespace Itinero.Tests.Routing.Flavours.Dijkstra;

public class BidirectionalDijkstraTests
{
    [Fact]
    public async Task BidirectionalDijkstra_OneToOne_OneHopShortest_ShouldFindOneHopPath()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1, vertex2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);

            edge = writer.AddEdge(vertex1, vertex2);
        }

        var latest = routerDb.Latest;
        var bidirectionalDijkstra = BidirectionalDijkstra.ForNetwork(latest);
        var (path, _) = await bidirectionalDijkstra.RunAsync(
            await latest.Snap().ToAsync(vertex1).FirstAsync(),
            await latest.Snap().ToAsync(vertex2).FirstAsync(),
            MockCostFunction.Create(1));

        Assert.NotNull(path);
        Assert.Equal(0, path.Offset1);
        Assert.Equal(ushort.MaxValue, path.Offset2);
        using var enumerator = path.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(edge, enumerator.Current.edge);
        Assert.True(enumerator.Current.forward);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public async Task BidirectionalDijkstra_OneToOne_TwoHopsShortest_ShouldFindTwoHopPath()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2;
        VertexId vertex1, vertex2, vertex3;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);

            edge1 = writer.AddEdge(vertex1, vertex2);
            edge2 = writer.AddEdge(vertex2, vertex3);
        }

        var latest = routerDb.Latest;
        var bidirectionalDijkstra = BidirectionalDijkstra.ForNetwork(latest);
        var (path, _) = await bidirectionalDijkstra.RunAsync(
            await latest.Snap().ToAsync(vertex1).FirstAsync(),
            await latest.Snap().ToAsync(vertex3).FirstAsync(),
            MockCostFunction.Create(1));

        Assert.NotNull(path);
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
}
