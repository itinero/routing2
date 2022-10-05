using Xunit;
using System.Linq;
using Itinero.Network;

namespace Itinero.Tests.Network.Mutation
{
    public class RoutingNetworkMutatorTests
    {
        [Fact]
        public void RouterNetwork_GetAsMutable_AddEdge_ShouldAddEdge()
        {
            var routerDb = new RouterDb();
            VertexId vertex1;
            VertexId vertex2;
            using (var mutable = routerDb.GetMutableNetwork()) {
                vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

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
            using (var mutable = routerDb.GetMutableNetwork()) {
                var vertex1 = mutable.AddVertex(
                    4.792613983154297,
                    51.26535213392538, (float?) null);
                var vertex2 = mutable.AddVertex(
                    4.797506332397461,
                    51.26674845584085, (float?) null);

                edge = mutable.AddEdge(vertex1, vertex2, new[] {
                    (4.795167446136475,
                        51.26580191532799, (float?) null)
                });
            }

            var routerDbLatest = routerDb.Latest;
            var enumerator = routerDbLatest.GetEdgeEnumerator();
            enumerator.MoveToEdge(edge);
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
            using (var mutable = routerDb.GetMutableNetwork()) {
                var vertex1 = mutable.AddVertex(
                    4.792613983154297,
                    51.26535213392538, (float?) null);
                var vertex2 = mutable.AddVertex(
                    4.797506332397461,
                    51.26674845584085, (float?) null);

                edge = mutable.AddEdge(vertex1, vertex2, attributes: new[] {("highway", "residential")});
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
            using (var mutable = routerDb.GetMutableNetwork()) {
                vertex1 = mutable.AddVertex(4.792613983154297, 51.26535213392538, (float?) null);
                vertex2 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);
                vertex3 = mutable.AddVertex(4.797506332397461, 51.26674845584085, (float?) null);

                edge1 = mutable.AddEdge(vertex1, vertex2);
                edge2 = mutable.AddEdge(vertex2, vertex3);

                mutable.AddTurnCosts(vertex2, Enumerable.Empty<(string key, string value)>(),
                    new[] {edge1, edge2}, new uint[,] {{1, 2}, {3, 4}});
            }

            var routerDbLatest = routerDb.Latest;
            var enumerator = routerDbLatest.GetEdgeEnumerator();

            // verify turn cost edge1 -> edge1.
            enumerator.MoveToEdge(edge1, true);
            var fromOrder = enumerator.HeadOrder;
            Assert.NotNull(fromOrder);
            var toOrder = fromOrder;
            Assert.NotNull(toOrder);
            enumerator.MoveToEdge(edge1, false);
            var cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
            Assert.Equal((byte) 1, cost.cost);

            // verify turn cost edge1 -> edge2.
            enumerator.MoveToEdge(edge1, true);
            fromOrder = enumerator.HeadOrder;
            Assert.NotNull(fromOrder);
            enumerator.MoveToEdge(edge2);
            toOrder = enumerator.TailOrder;
            Assert.NotNull(toOrder);
            cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
            Assert.Equal((byte) 2, cost.cost);

            // verify turn cost edge2 -> edge1.
            enumerator.MoveToEdge(edge2, false);
            fromOrder = enumerator.HeadOrder;
            Assert.NotNull(fromOrder);
            enumerator.MoveToEdge(edge1, false);
            toOrder = enumerator.TailOrder;
            Assert.NotNull(toOrder);
            cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
            Assert.Equal((byte) 3, cost.cost);

            // verify turn cost edge2 -> edge2.
            enumerator.MoveToEdge(edge2, false);
            fromOrder = enumerator.HeadOrder;
            Assert.NotNull(fromOrder);
            toOrder = fromOrder;
            Assert.NotNull(toOrder);
            enumerator.MoveToEdge(edge2, true);
            cost = enumerator.GetTurnCostToTail(fromOrder.Value).First();
            Assert.Equal((byte) 4, cost.cost);
        }
    }
}