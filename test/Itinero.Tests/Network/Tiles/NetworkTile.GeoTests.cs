using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles {
    public class NetworkTile_GeoTests {
        [Fact]
        public void NetworkTile_AddVertex0_ShouldStoreCoordinates() {
            var networkTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 =
                networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

            Assert.True(networkTile.TryGetVertex(vertex1, out var longitude, out var latitude, out _));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude, 4);
        }

        [Fact]
        public void NetworkTile_AddVertex0_ShouldStoreElevation() {
            var networkTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 =
                networkTile.AddVertex(4.7868, 51.2643, 156); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

            Assert.True(networkTile.TryGetVertex(vertex1, out _, out _, out var elevation));
            Assert.Equal(156, elevation);
        }

        [Fact]
        public void NetworkTile_AddVertex1_ShouldStoreElevation() {
            var networkTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            networkTile.AddVertex(4.7868, 51.2643, 156);
            var vertex2 = networkTile.AddVertex(4.7868, 51.2643, 1541);

            Assert.True(networkTile.TryGetVertex(vertex2, out _, out _, out var elevation));
            Assert.Equal(1541, elevation);
        }

        [Fact]
        public void NetworkTile_AddEdge0_EmptyShape_ShouldStoreEdge() {
            var networkTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
            var vertex2 = networkTile.AddVertex(4.86737, 51.267849);

            var edge = networkTile.AddEdge(vertex1, vertex2,
                System.Array.Empty<(double longitude, double latitude, float? e)>());
            Assert.Equal((uint) 0, edge.LocalId);
        }

        [Fact]
        public void NetworkTile_AddEdge0_OneShapePoint_ShouldStoreShapePoints() {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2, new[] {
                (4.86786,
                    51.26909, (float?) null)
            });

            var enumerator = new NetworkTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(edge, true));
            var shapes = enumerator.Shape;
            Assert.NotNull(shapes);
            Assert.Single(shapes);
            var shapePoint = shapes.First();
            Assert.Equal(4.86786, shapePoint.longitude, 4);
            Assert.Equal(51.26909, shapePoint.latitude, 4);
            Assert.Null(shapePoint.e);
        }

        [Fact]
        public void NetworkTile_AddEdge0_OneShapePoint_Elevation_ShouldStoreShapeElevation() {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728, 100);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849, 101);

            var edge = graphTile.AddEdge(vertex1, vertex2, new[] {
                (4.86786,
                    51.26909, (float?) 102f)
            });

            var enumerator = new NetworkTileEnumerator();
            enumerator.MoveTo(graphTile);
            Assert.True(enumerator.MoveTo(edge, true));
            var shapes = enumerator.Shape;
            Assert.NotNull(shapes);
            Assert.Single(shapes);
            var shapePoint = shapes.First();
            Assert.Equal(4.86786, shapePoint.longitude, 4);
            Assert.Equal(51.26909, shapePoint.latitude, 4);
            Assert.NotNull(shapePoint.e);
            Assert.Equal(102f, shapePoint.e.Value);
        }

        [Fact]
        public void NetworkTile_AddEdge0_ThreeShapePoints_ShouldStoreThreeShapePoints() {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

            var edge = graphTile.AddEdge(vertex1, vertex2, new[] {
                (
                    4.867324233055115,
                    51.269695361396586, (float?) null
                ),
                (
                    4.867860674858093,
                    51.26909794023487, (float?) null
                ),
                (
                    4.868037700653076,
                    51.26838639478469, (float?) null
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

        [Fact]
        public void NetworkTile_AddEdge0_ThreeShapePoints_Elevation_ShouldStoreElevation() {
            var graphTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = graphTile.AddVertex(4.86638, 51.269728, 100);
            var vertex2 = graphTile.AddVertex(4.86737, 51.267849, 110);

            var edge = graphTile.AddEdge(vertex1, vertex2, new[] {
                (
                    4.867324233055115,
                    51.269695361396586, (float?) 101
                ),
                (
                    4.867860674858093,
                    51.26909794023487, (float?) 106
                ),
                (
                    4.868037700653076,
                    51.26838639478469, (float?) 109
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
            Assert.NotNull(shapePoint.e);
            Assert.Equal(101f, shapePoint.e.Value);
            shapePoint = shapes[1];
            Assert.Equal(4.867860674858093, shapePoint.longitude, 4);
            Assert.Equal(51.26909794023487, shapePoint.latitude, 4);
            Assert.NotNull(shapePoint.e);
            Assert.Equal(106f, shapePoint.e.Value);
            shapePoint = shapes[2];
            Assert.Equal(4.868037700653076, shapePoint.longitude, 4);
            Assert.Equal(51.26838639478469, shapePoint.latitude, 4);
            Assert.NotNull(shapePoint.e);
            Assert.Equal(109f, shapePoint.e.Value);
        }
    }
}