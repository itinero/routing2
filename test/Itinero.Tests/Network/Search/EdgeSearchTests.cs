using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Search;
using Itinero.Network.Search.Edges;
using Xunit;

namespace Itinero.Tests.Network.Search;

public class EdgeSearchTests
{
    [Fact]
    public void EdgeSearch_SearchEdgesInBox_ShouldReturnNothingWhenNoEdges()
    {
        var routerDb = new RouterDb();
        using (var writer = routerDb.GetMutableNetwork())
        {
            writer.AddVertex(4.792613983154297, 51.26535213392538);
            writer.AddVertex(4.797506332397461, 51.26674845584085);
        }

        var edges = routerDb.Latest.SearchEdgesInBox(((4.796, 51.267, null), (4.798, 51.265, null)));
        Assert.NotNull(edges);
        Assert.False(edges.MoveNext());
    }

    [Fact]
    public void EdgeSearch_SearchEdgesInBox_ShouldReturnEdgeWhenOneVertexInBox()
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

        var edges = routerDb.Latest.SearchEdgesInBox(((4.796, 51.267, null), (4.798, 51.265, null)));
        Assert.NotNull(edges);
        Assert.True(edges.MoveNext());
        Assert.Equal(edge, edges.EdgeId);
        Assert.False(edges.MoveNext());
    }

    [Fact]
    public async Task EdgeSearch_SnapInBox_ShouldSnapToVertex1WhenVertex1Closest()
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

        var snapPoint = await routerDb.Latest.SnapInBoxAsync(((4.792613983154297 - 0.001, 51.26535213392538 + 0.001, null),
            (4.792613983154297 + 0.001, 51.26535213392538 - 0.001, null)));
        Assert.Equal(edge, snapPoint.EdgeId);
        Assert.Equal(0, snapPoint.Offset);
    }

    [Fact]
    public async Task EdgeSearch_SnapInBox_ShouldSnapToVertex2WhenVertex2Closest()
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

        var snapPoint = await routerDb.Latest.SnapInBoxAsync(((4.797506332397461 - 0.001, 51.26674845584085 + 0.001, null),
            (4.797506332397461 + 0.001, 51.26674845584085 - 0.001, null)));
        Assert.Equal(edge, snapPoint.EdgeId);
        Assert.Equal(ushort.MaxValue, snapPoint.Offset);
    }

    [Fact]
    public async Task EdgeSearch_SnapInBox_ShouldSnapToSegmentWhenMiddleIsClosest()
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

        (double lon, double lat) middle = ((4.79261398315429 + 4.797506332397461) / 2, (51.26535213392538 + 51.26674845584085) / 2);
        var snapPoint = await routerDb.Latest.SnapInBoxAsync(((middle.lon - 0.01, middle.lat + 0.01, null),
            (middle.lon + 0.01, middle.lat - 0.01, null)));
        Assert.Equal(edge, snapPoint.EdgeId);
    }

    [Fact]
    public async Task EdgeSearch_SnapInBox_ShouldSnapToClosestSegment()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2;
        VertexId vertex1, vertex2, vertex3, vertex4;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.796154499053955, 51.26912479079087);
            vertex2 = writer.AddVertex(4.799630641937256, 51.27015852526688);
            vertex3 = writer.AddVertex(4.796798229217529, 51.26835954379726);
            vertex4 = writer.AddVertex(4.800124168395996, 51.26937987029022);
            edge1 = writer.AddEdge(vertex1, vertex2);
            edge2 = writer.AddEdge(vertex3, vertex4);
        }

        var snapPoint = await routerDb.Latest.SnapInBoxAsync(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01, null),
            (4.798600673675537 + 0.01, 51.268748881579405 - 0.01, null)));
        Assert.Equal(edge2, snapPoint.EdgeId);
    }

    [Fact]
    public async Task EdgeSearch_SnapAllInBox_NonOrthogonal_ShouldSnapToAll()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2, edge3;
        VertexId vertex1, vertex2, vertex3, vertex4, vertex5, vertex6;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.796154499053955, 51.26912479079087);
            vertex2 = writer.AddVertex(4.799630641937256, 51.27015852526688);
            vertex3 = writer.AddVertex(4.796798229217529, 51.26835954379726);
            vertex4 = writer.AddVertex(4.800124168395996, 51.26937987029022);
            vertex5 = writer.AddVertex(4.799898862838745, 51.269762486884446);
            vertex6 = writer.AddVertex(4.802924394607544, 51.27068880860352);
            edge1 = writer.AddEdge(vertex1, vertex2);
            edge2 = writer.AddEdge(vertex3, vertex4);
            edge3 = writer.AddEdge(vertex5, vertex6);
        }

        var snapPoints = await routerDb.Latest.SnapAllInBoxAsync(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01, null),
            (4.798600673675537 + 0.01, 51.268748881579405 - 0.01, null)), nonOrthogonalEdges: true).ToListAsync();
        Assert.True(snapPoints.Exists(x => x.EdgeId == edge1));
        Assert.True(snapPoints.Exists(x => x.EdgeId == edge2));
        Assert.True(snapPoints.Exists(x => x.EdgeId == edge3));
    }

    [Fact]
    public async Task EdgeSearch_SnapAllInBox_ShouldSnapToOrthogonal()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2, edge3;
        VertexId vertex1, vertex2, vertex3, vertex4, vertex5, vertex6;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.796154499053955, 51.26912479079087);
            vertex2 = writer.AddVertex(4.799630641937256, 51.27015852526688);
            vertex3 = writer.AddVertex(4.796798229217529, 51.26835954379726);
            vertex4 = writer.AddVertex(4.800124168395996, 51.26937987029022);
            vertex5 = writer.AddVertex(4.799898862838745, 51.269762486884446);
            vertex6 = writer.AddVertex(4.802924394607544, 51.27068880860352);
            edge1 = writer.AddEdge(vertex1, vertex2);
            edge2 = writer.AddEdge(vertex3, vertex4);
            edge3 = writer.AddEdge(vertex5, vertex6);
        }

        var snapPoints = await routerDb.Latest.SnapAllInBoxAsync(((4.798600673675537 - 0.01, 51.268748881579405 + 0.01, null),
            (4.798600673675537 + 0.01, 51.268748881579405 - 0.01, null)), nonOrthogonalEdges: false).ToListAsync();
        Assert.True(snapPoints.Exists(x => x.EdgeId == edge1));
        Assert.True(snapPoints.Exists(x => x.EdgeId == edge2));
        Assert.False(snapPoints.Exists(x => x.EdgeId == edge3));
    }
}

