using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles
{
    public partial class NetworkTileTests
    {
        [Fact]
        public void NetworkTile_AddEdge0_OneShapePoint_ShouldStoreShapePoints()
        {
            var graphTile = new NetworkTile(14, 
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2, new []{ (4.86786,
                51.26909)});

            var enumerator = new NetworkTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(edge, true));
            var shapes = enumerator.Shape;
            Assert.NotNull(shapes);
            Assert.Single(shapes);
            var shapePoint = shapes.First();
            Assert.Equal(4.86786, shapePoint.longitude, 4);
            Assert.Equal(51.26909, shapePoint.latitude, 4);
        }

        [Fact]
        public void NetworkTile_AddEdge0_ThreeShapePoint_ShouldStoreThreeShapePoints()
        {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
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

            var enumerator = new NetworkTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(edge, true));
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
    }
}