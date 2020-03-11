using Itinero.Data.Graphs;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs
{
    public class GraphTileEnumeratorTests
    {
        [Fact]
        public void GraphTileEnumerator_OneVertex_MoveToVertex_ShouldReturnTrue()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex = graphTile.AddVertex(4.86638, 51.269728);

            var enumerator =  new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(vertex));
        }

        [Fact]
        public void GraphTileEnumerator_OneEdge_MoveToVertex1_ShouldReturnOneEdgeForward()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2);

            var enumerator =  new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.True(enumerator.Forward);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal(edge.LocalId, enumerator.EdgeId);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void GraphTileEnumerator_OneEdge_MoveToVertex2_ShouldReturnOneEdgeBackward()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2);

            var enumerator =  new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            enumerator.MoveTo(vertex2);
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.Forward);
            Assert.Equal(vertex2, enumerator.Vertex1);
            Assert.Equal(vertex1, enumerator.Vertex2);
            Assert.Equal(edge.LocalId, enumerator.EdgeId);
            Assert.False(enumerator.MoveNext());
        }
    }
}