using System.Linq;
using Itinero.Data;
using Itinero.LocalGeo;
using NUnit.Framework;

namespace Itinero.Tests.Data
{
    public class NetworkTests
    {
        [Test]
        public void NetworkGraphEnumerator_ShouldEnumerateEdgesInGraph()
        {
            var network = new Network();
            var vertex1 = network.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = network.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = network.AddEdge(vertex1, vertex2);

            var enumerator = network.GetEdgeEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(vertex1, enumerator.From);
            Assert.AreEqual(vertex2, enumerator.To);
            Assert.AreEqual(true, enumerator.Forward);
            Assert.AreEqual(0, enumerator.CopyDataTo(new byte[10]));
            Assert.AreEqual(0, enumerator.Data.Length);
        }

        [Test]
        public void Network_ShouldStoreShape()
        {
            var network = new Network();
            var vertex1 = network.AddVertex(
                4.792613983154297,
                51.26535213392538);
            var vertex2 = network.AddVertex(
                4.797506332397461,
                51.26674845584085);

            var edgeId = network.AddEdge(vertex1, vertex2, shape: new [] { new Coordinate(4.795167446136475,
                51.26580191532799), });

            var shape = network.GetShape(edgeId);
            Assert.IsNotNull(shape);
            var shapeList = shape.ToList();
            Assert.AreEqual(1, shapeList.Count);
            Assert.AreEqual(4.795167446136475, shapeList[0].Longitude);
            Assert.AreEqual(51.26580191532799, shapeList[0].Latitude);
        }
    }
}