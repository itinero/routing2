using System.Linq;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Network.Mutation;

public class RoutingNetworkMutatorTests
{
    [Fact]
    public void RouterNetwork_GetAsMutable_AddEdge_ShouldAddEdge()
    {
        var provider = TestProvider.New();
        var routerDb = provider.RouterDb;
        VertexId vertex1;
        VertexId vertex2;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            mutable.AddEdge(vertex1, vertex2, 0);
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
    public void RouterDb_GetAsMutable_AddTurnCosts_ShouldStoreTurnCosts()
    {
        var provider = TestProvider.New();
        var routerDb = provider.RouterDb;
        EdgeId edge1, edge2;
        VertexId vertex1, vertex2, vertex3;
        using (var mutable = routerDb.GetMutableNetwork())
        {
            vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);
            vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?)null);

            edge1 = mutable.AddEdge(vertex1, vertex2, 0);
            edge2 = mutable.AddEdge(vertex2, vertex3, 0);

            mutable.AddTurnCosts(vertex2,
                new[] { edge1, edge2 }, new uint[,] { { 1, 2 }, { 3, 4 } }, 0);
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








}
