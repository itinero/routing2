using System.Linq;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Network.Mutation;

public class RoutingNetworkMutatorTests
{
    [Fact]
    public void RouterNetwork_GetAsMutable_AddEdge_ShouldAddEdge()
    {
        var routerDb = new RouterDb();
        VertexId vertex1;
        VertexId vertex2;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            mutable.AddEdge(vertex1, vertex2);
        }

        var routerDbLatest = routerDb.Latest;
        var enumerator = routerDbLatest.GetEdgeEnumerator();
        enumerator.MoveTo(vertex1);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(vertex1, enumerator.Tail);
        Assert.Equal(vertex2, enumerator.Head);
        Assert.True(enumerator.Forward);
    }

    [Fact]
    public void RouterDb_GetAsMutable_AddEdgeWithShape_ShouldStoreShape()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            var vertex1 = mutable.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = mutable.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            edge = mutable.AddEdge(vertex1, vertex2, new[] {
                    (4.795167446136475,
                        51.26580191532799, (float?) null)
                });
        }

        var routerDbLatest = routerDb.Latest;
        var enumerator = routerDbLatest.GetEdgeEnumerator();
        enumerator.MoveTo(edge);
        var shape = enumerator.Shape;
        Assert.NotNull(shape);
        var shapeList = shape.ToList();
        Assert.Single(shapeList);
        Assert.Equal(4.795167446136475, shapeList[0].longitude, 4);
        Assert.Equal(51.26580191532799, shapeList[0].latitude, 4);
    }

    [Fact]
    public void RouterDb_GetAsMutable_AddEdgeWithAttributes_ShouldStoreAttributes()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            var vertex1 = mutable.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = mutable.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            edge = mutable.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
        }

        var attributes = routerDb.Latest.GetAttributes(edge);
        Assert.NotNull(attributes);
        Assert.Single(attributes);
        Assert.Equal("highway", attributes.First().key);
        Assert.Equal("residential", attributes.First().value);
    }

    [Fact]
    public void RouterDb_GetAsMutable_AddTurnCosts_ShouldStoreTurnCosts()
    {
        var routerDb = new RouterDb();
        EdgeId edge1, edge2;
        VertexId vertex1, vertex2, vertex3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);
            vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge1 = mutable.AddEdge(vertex1, vertex2);
            edge2 = mutable.AddEdge(vertex2, vertex3);

            mutable.AddTurnCosts(vertex2, Enumerable.Empty<(string key, string value)>(),
                new[] { edge1, edge2 }, new uint[,] { { 1, 2 }, { 3, 4 } });
        }

        var routerDbLatest = routerDb.Latest;
        var enumerator = routerDbLatest.GetEdgeEnumerator();

        // verify turn cost edge1 -> edge1.
        enumerator.MoveTo(edge1, true);
        var fromOrder = enumerator.HeadOrder;
        Assert.NotNull(fromOrder);
        var toOrder = fromOrder;
        Assert.NotNull(toOrder);
        enumerator.MoveTo(edge1, false);
        var cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
        Assert.Equal((byte)1, cost.cost);

        // verify turn cost edge1 -> edge2.
        enumerator.MoveTo(edge1, true);
        fromOrder = enumerator.HeadOrder;
        Assert.NotNull(fromOrder);
        enumerator.MoveTo(edge2);
        toOrder = enumerator.TailOrder;
        Assert.NotNull(toOrder);
        cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
        Assert.Equal((byte)2, cost.cost);

        // verify turn cost edge2 -> edge1.
        enumerator.MoveTo(edge2, false);
        fromOrder = enumerator.HeadOrder;
        Assert.NotNull(fromOrder);
        enumerator.MoveTo(edge1, false);
        toOrder = enumerator.TailOrder;
        Assert.NotNull(toOrder);
        cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
        Assert.Equal((byte)3, cost.cost);

        // verify turn cost edge2 -> edge2.
        enumerator.MoveTo(edge2, false);
        fromOrder = enumerator.HeadOrder;
        Assert.NotNull(fromOrder);
        toOrder = fromOrder;
        Assert.NotNull(toOrder);
        enumerator.MoveTo(edge2, true);
        cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
        Assert.Equal((byte)4, cost.cost);
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_DeleteEdge_ShouldRemoveEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);
        }

        var edgeEnumerator = routerDb.Latest.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(vertex1);
        Assert.False(edgeEnumerator.MoveNext());
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_DeleteEdge_ShouldNotEnumerateEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);

            var edgeEnumerator = mutator.GetEdgeEnumerator();
            edgeEnumerator.MoveTo(vertex1);
            Assert.False(edgeEnumerator.MoveNext());
        }
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_TwoEdges_DeleteSecondEdge_ShouldNotEnumerateSecondEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "not-residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);

            var edgeEnumerator = mutator.GetEdgeEnumerator();
            edgeEnumerator.MoveTo(vertex1);
            Assert.True(edgeEnumerator.MoveNext());
            Assert.True(edgeEnumerator.Attributes.Contains(("highway", "residential")));
            Assert.False(edgeEnumerator.MoveNext());
        }
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_TwoEdges_DeleteFirstEdge_ShouldNotEnumerateFirstEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
            writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "not-residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);

            var edgeEnumerator = mutator.GetEdgeEnumerator();
            edgeEnumerator.MoveTo(vertex1);
            Assert.True(edgeEnumerator.MoveNext());
            Assert.True(edgeEnumerator.Attributes.Contains(("highway", "not-residential")));
            Assert.False(edgeEnumerator.MoveNext());
        }
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_TwoEdges_DeleteFirstEdge_ShouldRemoveFirstEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
            writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "not-residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);
        }

        var edgeEnumerator = routerDb.Latest.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(vertex1);
        Assert.True(edgeEnumerator.MoveNext());
        Assert.True(edgeEnumerator.Attributes.Contains(("highway", "not-residential")));
        Assert.False(edgeEnumerator.MoveNext());
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_TwoEdges_DeleteSecondEdge_ShouldRemoveSecondEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.792613983154297,
                51.26535213392538, (float?)null);
            var vertex2 = writer.AddVertex(
                4.797506332397461,
                51.26674845584085, (float?)null);

            writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "not-residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);
        }

        var edgeEnumerator = routerDb.Latest.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(vertex1);
        Assert.True(edgeEnumerator.MoveNext());
        Assert.True(edgeEnumerator.Attributes.Contains(("highway", "residential")));
        Assert.False(edgeEnumerator.MoveNext());
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_DeleteCrossTileEdge_ShouldNotEnumerateEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.785726659345158,
                51.263701222867866, (float?)null);
            var vertex2 = writer.AddVertex(
                4.8079561067168015,
                51.26376332008826, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);

            var edgeEnumerator = mutator.GetEdgeEnumerator();
            edgeEnumerator.MoveTo(vertex1);
            Assert.False(edgeEnumerator.MoveNext());
        }
    }
    
    [Fact]
    public void RouterDb_GetAsMutable_DeleteCrossTileEdge_ShouldRemoveEdge()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        VertexId vertex1;
        using (var writer = routerDb.Latest.GetWriter())
        {
            vertex1 = writer.AddVertex(
                4.785726659345158,
                51.263701222867866, (float?)null);
            var vertex2 = writer.AddVertex(
                4.8079561067168015,
                51.26376332008826, (float?)null);

            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("highway", "residential") });
        }
        
        // delete edge.
        using (var mutator = routerDb.GetMutableNetwork())
        {
            mutator.DeleteEdge(edge);
        }

        var edgeEnumerator = routerDb.Latest.GetEdgeEnumerator();
        edgeEnumerator.MoveTo(vertex1);
        Assert.False(edgeEnumerator.MoveNext());
    }
}
