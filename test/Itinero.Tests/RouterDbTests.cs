using System.IO;
using System.Linq;
using Xunit;

namespace Itinero.Tests
{
    public class RouterDbTests
    {
        [Fact]
        public void RouterDbGraphEnumerator_ShouldEnumerateEdgesInGraph()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = routerDb.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = routerDb.AddEdge(vertex1, vertex2);

            var enumerator = routerDb.GetEdgeEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
        }

        [Fact]
        public void RouterDb_ShouldStoreShape()
        {
            var network = new RouterDb();
            var vertex1 = network.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = network.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edge = network.AddEdge(vertex1, vertex2, shape: new [] { (4.795167446136475,
                51.26580191532799) });

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveToEdge(edge);
            var shape = enumerator.Shape;
            Assert.NotNull(shape);
            var shapeList = shape.ToList();
            Assert.Single(shapeList);
            Assert.Equal(4.795167446136475, shapeList[0].longitude, 4);
            Assert.Equal(51.26580191532799, shapeList[0].latitude, 4);
        }

        [Fact]
        public void RouterDb_ShouldStoreAttributes()
        {
            var routerDb = new RouterDb();
            var vertex1 = routerDb.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = routerDb.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edge = routerDb.AddEdge(vertex1, vertex2, attributes: new [] { ("highway", "residential") });

            var attributes = routerDb.GetAttributes(edge);
            Assert.NotNull(attributes);
            Assert.Single(attributes);
            Assert.Equal("highway", attributes.First().key);
            Assert.Equal("residential", attributes.First().value);
        }
    }
}