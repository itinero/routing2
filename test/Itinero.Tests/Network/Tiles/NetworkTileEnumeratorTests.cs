using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles
{
    public class NetworkTile_EnumeratorTests
    {
        [Fact]
        public void NetworkTileEnumerator_OneVertex_MoveToVertex_ShouldReturnTrue()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex = networkTile.AddVertex(4.86638, 51.269728, (float?)null);

            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            Assert.True(enumerator.MoveTo(vertex));
        }

        [Fact]
        public void NetworkTileEnumerator_OneEdge_MoveToVertex1_ShouldReturnOneEdgeForward()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728, (float?)null);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849, (float?)null);

            var edge = networkTile.AddEdge(vertex1, vertex2);

            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex1);
            Assert.True(enumerator.MoveNext());
            Assert.True(enumerator.Forward);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void NetworkTileEnumerator_OneEdge_MoveToVertex2_ShouldReturnOneEdgeBackward()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728, (float?)null);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849, (float?)null);

            var edge = networkTile.AddEdge(vertex1, vertex2);

            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            enumerator.MoveTo(vertex2);
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.Forward);
            Assert.Equal(vertex2, enumerator.Vertex1);
            Assert.Equal(vertex1, enumerator.Vertex2);
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void NetworkTileEnumerator_OneEdge_ThreeShapePoint_ShouldReturnThreeShapePoints()
        {
            var networkTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728, (float?)null);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849, (float?)null);

            var edge = networkTile.AddEdge(vertex1, vertex2, new[]
            {
                (
                    4.867324233055115,
                    51.269695361396586, (float?)null
                ),
                (
                    4.867860674858093,
                    51.26909794023487, (float?)null
                ),
                (
                    4.868037700653076,
                    51.26838639478469, (float?)null
                )
            });

            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
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
        public void NetworkTileEnumerator_OneEdge_MoveToEdgeForward_ShouldMoveTo_EdgeForward()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728, (float?)null);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849, (float?)null);

            var edge = networkTile.AddEdge(vertex1, vertex2);

            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            Assert.True(enumerator.MoveTo(edge, true));
            Assert.True(enumerator.Forward);
            Assert.Equal(vertex1, enumerator.Vertex1);
            Assert.Equal(vertex2, enumerator.Vertex2);
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void NetworkTileEnumerator_OneEdge_MoveToEdgeBackward_ShouldMoveTo_EdgeBackward()
        {
            var networkTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728, (float?)null);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849, (float?)null);

            var edge = networkTile.AddEdge(vertex1, vertex2);

            var enumerator =  new NetworkTileEnumerator();
            enumerator.MoveTo(networkTile);
            Assert.True(enumerator.MoveTo(edge, false));
            Assert.False(enumerator.Forward);
            Assert.Equal(vertex2, enumerator.Vertex1);
            Assert.Equal(vertex1, enumerator.Vertex2);
            Assert.Equal(edge, enumerator.EdgeId);
            Assert.False(enumerator.MoveNext());
        }
    }
}