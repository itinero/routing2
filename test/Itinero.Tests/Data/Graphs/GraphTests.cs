using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs
{
    public class GraphTests
    {
        [Fact]
        public void Graph_ShouldGenerateTiledVertexId()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId);
            Assert.Equal((uint)0, vertex1.LocalId);
            
            // when adding the vertex a second time it should generate the same tile but a new local id.
            
            vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
            Assert.Equal((uint)89546969, vertex1.TileId); 
            Assert.Equal((uint)1, vertex1.LocalId);
        }
        
        [Fact]
        public void Graph_ShouldGenerateTiledVertexIdWithProperLocalId()
        {
            // when adding a vertex to a tile the graph should always generate an id in the same tile with a proper
            // local id.
            
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

            var tile = Tile.FromLocalId(vertex1.TileId, graph.Zoom);
            Assert.Equal((uint)8409, tile.X);
            Assert.Equal((uint)5465, tile.Y);
            Assert.Equal(14, tile.Zoom);
        }

        [Fact]
        public void Graph_ShouldAddEdgeAndReturnId0()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = graph.AddEdge(vertex1, vertex2);
            Assert.Equal((uint)0, edgeId); // first edge should have id 0.
        }

        [Fact]
        public void Graph_ShouldAddEdgeSecondEdgeAndReturnId1()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId1 = graph.AddEdge(vertex1, vertex2);
            var edgeId2 = graph.AddEdge(vertex1, vertex3);
            Assert.Equal((uint)1, edgeId2);
        }

        [Fact]
        public void GraphEdgeEnumerator_ShouldMoveToVertexEvenWhenNoEdges()
        {
            var graph = new Graph();
            var vertex = graph.AddVertex(4.792613983154297, 51.26535213392538);

            var enumerator = graph.GetEnumerator();
            Assert.True(enumerator.MoveTo(vertex));
        }

        [Fact]
        public void GraphEdgeEnumerator_MoveToShouldReturnFalseWhenNoEdges()
        {
            var graph = new Graph();
            var vertex = graph.AddVertex(4.792613983154297, 51.26535213392538);

            var enumerator = graph.GetEnumerator();
            enumerator.MoveTo(vertex);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void GraphEdgeEnumerator_MoveToShouldMoveToFirstEdge()
        {
            var graph = new Graph();
            var vertex1 = graph.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = graph.AddVertex(4.797506332397461, 51.26674845584085);

            var edgeId = graph.AddEdge(vertex1, vertex2);

            var enumerator = graph.GetEnumerator();
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(vertex1, enumerator.From);
            Assert.Equal(vertex2, enumerator.To);
            Assert.True(enumerator.Forward);
            Assert.Equal(0, enumerator.CopyDataTo(new byte[10]));
            Assert.Empty(enumerator.Data);
        }
    }
}