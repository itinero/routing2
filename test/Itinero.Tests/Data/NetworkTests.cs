using System.Linq;
using Itinero.Data;
using Itinero.Data.Graphs;
using Itinero.LocalGeo;
using Xunit;

namespace Itinero.Tests.Data
{
    public class NetworkTests
    {
        [Fact]
        public void NetworkGraphEnumerator_ShouldEnumerateEdgesInGraph()
        {
            var network = new Graph();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = network.AddEdge(vertex1, vertex2);

            var enumerator = network.GetEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
        }
//
//        [Fact]
//        public void Network_ShouldStoreShape()
//        {
//            var network = new Graph();
//            var vertex1 = network.AddVertex(
//                4.792613983154297,
//                51.26535213392538);
//            var vertex2 = network.AddVertex(
//                4.797506332397461,
//                51.26674845584085);
//
//            var edgeId = network.AddEdge(vertex1, vertex2, shape: new [] { new Coordinate(4.795167446136475,
//                51.26580191532799), });
//
//            var enumerator = network.GetEnumerator();
//            enumerator.MoveToEdge(edgeId);
//            var shape = enumerator.GetShape();
//            Assert.NotNull(shape);
//            var shapeList = shape.ToList();
//            Assert.Single(shapeList);
//            Assert.Equal(4.795167446136475, shapeList[0].Longitude);
//            Assert.Equal(51.26580191532799, shapeList[0].Latitude);
//        }
    }
}