using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using NUnit.Framework;

namespace Itinero.Tests.Data.Graphs
{
    [TestFixture]
    public class GraphTests
    {
        [Test]
        public void Graph_ShouldGenerateTiledVertexId()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.AreEqual(89546969, vertex1.TileId);
            Assert.AreEqual(0, vertex1.LocalId);
            
            // when adding the vertex a second time it should generate the same tile but a new local id.
            
            vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.AreEqual(89546969, vertex1.TileId); 
            Assert.AreEqual(1, vertex1.LocalId);
        }
        
        [Test]
        public void Graph_ShouldGenerateTiledVertexIdWithProperLocalId()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile with a proper
            // local id.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

            var tile = Tile.FromLocalId(vertex1.TileId, graph.Zoom);
            Assert.AreEqual(8409, tile.X);
            Assert.AreEqual(5465, tile.Y);
            Assert.AreEqual(14, tile.Zoom);
        }

        [Test]
        public void Graph_ShouldAddEdgeAndReturnId0()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = graph.AddEdge(vertex1, vertex2);
            Assert.AreEqual(0, edgeId); // first edge should have id 0.
        }

        [Test]
        public void Graph_ShouldAddEdgeSecondEdgeAndReturnId1()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId1 = graph.AddEdge(vertex1, vertex2);
            var edgeId2 = graph.AddEdge(vertex1, vertex3);
            Assert.AreEqual(1, edgeId2);
        }
    }
}