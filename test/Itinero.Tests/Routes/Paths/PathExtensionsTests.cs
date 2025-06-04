using System.Linq;
using Itinero.Network;
using Itinero.Routes.Paths;
using Xunit;

namespace Itinero.Tests.Routes.Paths;

public class PathExtensionsTests
{
    [Fact]
    public void PathExtensions_InvertDirection_SingleEdge_ShouldInvertForwardAndOffsets()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2);
        }

        var path = new Path(routerDb.Latest);
        path.Append(edge, true);

        var inverted = path.InvertDirection().ToList();
        Assert.Single(inverted);
        Assert.Equal(edge, inverted.First().edge);
        Assert.False(inverted.First().forward);
        Assert.Equal(0, inverted.First().offset1);
        Assert.Equal(ushort.MaxValue, inverted.First().offset2);
    }

    [Fact]
    public void PathExtensions_InvertDirection_TwoEdgesForward_ShouldReorderAndInvertEdges()
    {
        var routerDb = new RouterDb();
        EdgeId edge1;
        EdgeId edge2;
        VertexId vertex1;
        VertexId vertex2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge1 = writer.AddEdge(vertex1, vertex2);
            edge2 = writer.AddEdge(vertex2, vertex3);
        }

        var path = new Path(routerDb.Latest);
        path.Append(edge1, true);
        path.Append(edge2, true);

        var inverted = path.InvertDirection().ToList();
        Assert.Equal(2, inverted.Count);

        Assert.Equal(edge2, inverted[0].edge);
        Assert.False(inverted[0].forward);
        Assert.Equal(0, inverted[0].offset1);
        Assert.Equal(ushort.MaxValue, inverted[0].offset2);

        Assert.Equal(edge1, inverted[1].edge);
        Assert.False(inverted[1].forward);
        Assert.Equal(0, inverted[1].offset1);
        Assert.Equal(ushort.MaxValue, inverted[1].offset2);
    }

    [Fact]
    public void PathExtensions_InvertDirection_TwoEdgesBackwardForward_ShouldReorderAndInvertEdges()
    {
        var routerDb = new RouterDb();
        EdgeId edge1;
        EdgeId edge2;
        VertexId vertex1;
        VertexId vertex2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge1 = writer.AddEdge(vertex2, vertex1);
            edge2 = writer.AddEdge(vertex2, vertex3);
        }

        var path = new Path(routerDb.Latest);
        path.Append(edge1, false);
        path.Append(edge2, true);

        var inverted = path.InvertDirection().ToList();
        Assert.Equal(2, inverted.Count);

        Assert.Equal(edge2, inverted[0].edge);
        Assert.False(inverted[0].forward);
        Assert.Equal(0, inverted[0].offset1);
        Assert.Equal(ushort.MaxValue, inverted[0].offset2);

        Assert.Equal(edge1, inverted[1].edge);
        Assert.True(inverted[1].forward);
        Assert.Equal(0, inverted[1].offset1);
        Assert.Equal(ushort.MaxValue, inverted[1].offset2);
    }

    [Fact]
    public void PathExtensions_InvertDirection_SingleEdge_OffsetsShouldInvert()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2);
        }

        var path = new Path(routerDb.Latest);
        path.Append(edge, true);
        path.Offset1 = 13107; // 20%
        path.Offset2 = 39321; // 60%

        var inverted = path.InvertDirection().ToList();
        Assert.Single(inverted);
        Assert.Equal(edge, inverted.First().edge);
        Assert.False(inverted.First().forward);
        Assert.Equal(ushort.MaxValue - 39321, inverted.First().offset1); // 40%
        Assert.Equal(ushort.MaxValue - 13107, inverted.First().offset2); // 80%
    }

    [Fact]
    public void PathExtensions_InvertDirection_TwoEdgesOffsets_OffsetsShouldInvert()
    {
        var routerDb = new RouterDb();
        EdgeId edge1;
        EdgeId edge2;
        VertexId vertex1;
        VertexId vertex2;
        using (var writer = routerDb.GetMutableNetwork())
        {
            vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge1 = writer.AddEdge(vertex2, vertex1);
            edge2 = writer.AddEdge(vertex2, vertex3);
        }

        var path = new Path(routerDb.Latest);
        path.Append(edge1, false);
        path.Append(edge2, true);
        path.Offset1 = 13107; // 20%
        path.Offset2 = 39321; // 60%

        var inverted = path.InvertDirection().ToList();
        Assert.Equal(2, inverted.Count);

        Assert.Equal(edge2, inverted[0].edge);
        Assert.False(inverted[0].forward);
        Assert.Equal(ushort.MaxValue - 39321, inverted[0].offset1); // 40%
        Assert.Equal(ushort.MaxValue, inverted[0].offset2);

        Assert.Equal(edge1, inverted[1].edge);
        Assert.True(inverted[1].forward);
        Assert.Equal(0, inverted[1].offset1);
        Assert.Equal(ushort.MaxValue - 13107, inverted[1].offset2); // 80%
    }
}
