using System.Linq;
using Itinero.Data.Graphs.Tiles;
using Itinero.Data.Tiles;
using Xunit;

namespace Itinero.Tests.Data.Graphs.Tiles
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
            Assert.Equal(edge.LocalId, enumerator.EdgeId.LocalId);
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
            Assert.Equal(edge.LocalId, enumerator.EdgeId.LocalId);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void GraphTileEnumerator_OneEdge_ThreeShapePoint_ShouldReturnThreeShapePoints()
        {
            var graphTile = new GraphTile(14,
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2, new[]
            {
                (
                    4.867324233055115,
                    51.269695361396586
                ),
                (
                    4.867860674858093,
                    51.26909794023487
                ),
                (
                    4.868037700653076,
                    51.26838639478469
                )
            });

            var enumerator =  new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            var shapes = enumerator.Shape.ToList();
            
            Assert.NotNull(shapes);
            Assert.Equal(3, shapes.Count);
            var shapePoint = shapes[0];
            Assert.Equal(4.867324233055115, shapePoint.longitude, 4);
            Assert.Equal(51.269695361396586, shapePoint.latitude, 4);
            shapePoint = shapes[1];
            Assert.Equal(4.867860674858093, shapePoint.longitude, 4);
            Assert.Equal(51.26909794023487, shapePoint.latitude, 4);
            shapePoint = shapes[2];
            Assert.Equal(4.868037700653076, shapePoint.longitude, 4);
            Assert.Equal(51.26838639478469, shapePoint.latitude, 4);
        }

        [Fact]
        public void GraphTileEnumerator_OneEdge_MoveToEdgeForward_ShouldMoveTo_EdgeForward()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2);

            var enumerator =  new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(edge, true));
            Assert.True(enumerator.Forward);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal(edge.LocalId, enumerator.EdgeId.LocalId);
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void GraphTileEnumerator_OneEdge_MoveToEdgeBackward_ShouldMoveTo_EdgeBackward()
        {
            var graphTile = new GraphTile(14, 
                Tile.WorldToTile(4.86638, 51.269728, 14).LocalId);
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2);

            var enumerator =  new GraphTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(edge, false));
            Assert.False(enumerator.Forward);
            Assert.Equal(vertex2, enumerator.Vertex1);
            Assert.Equal(vertex1, enumerator.Vertex2);
            Assert.Equal(edge.LocalId, enumerator.EdgeId.LocalId);
            Assert.False(enumerator.MoveNext());
        }
    }
}