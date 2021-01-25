using System.IO;
using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles
{
    public class NetworkTile_SerializationTests
    {
        [Fact]
        public void NetworkTile_Serialize_Deserialize_OneVertex()
        {
            var expectedTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 =
                expectedTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

            var memoryStream = new MemoryStream();
            expectedTile.WriteTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var graphTile = NetworkTile.ReadFrom(memoryStream);
            Assert.Equal((uint) 89546969, graphTile.TileId);
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude, out _));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude, 4);
        }
        
        [Fact]
        public void NetworkTile_Serialize_Deserialize_OneVertex_Elevation()
        {
            var expectedTile = new NetworkTile(14,
                TileStatic.ToLocalId(4.7868, 51.2643, 14));
            var vertex1 =
                expectedTile.AddVertex(4.7868, 51.2643, 4137); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

            var memoryStream = new MemoryStream();
            expectedTile.WriteTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var graphTile = NetworkTile.ReadFrom(memoryStream);
            Assert.Equal((uint) 89546969, graphTile.TileId);
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude, out var e));
            Assert.Equal(4.7868, longitude, 4);
            Assert.Equal(51.2643, latitude, 4);
            Assert.NotNull(e);
            Assert.Equal(4137, e.Value, 4);
        }

        [Fact]
        public void NetworkTile_Serialize_Deserialize_OneEdge()
        {
            var expected = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = expected.AddVertex(4.86638, 51.269728);
            var vertex2 = expected.AddVertex(4.86737, 51.267849);
            var edge = expected.AddEdge(vertex1, vertex2);

            var memoryStream = new MemoryStream();
            expected.WriteTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var graphTile = NetworkTile.ReadFrom(memoryStream);
            Assert.Equal((uint) 89546973, graphTile.TileId);
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude, out _));
            Assert.Equal(4.86638, longitude, 4);
            Assert.Equal(51.269728, latitude, 4);
            Assert.True(graphTile.TryGetVertex(vertex2, out longitude, out latitude, out _));
            Assert.Equal(4.86737, longitude, 4);
            Assert.Equal(51.267849, latitude, 3);

            var edgeEnumerator = new NetworkTileEnumerator();
            edgeEnumerator.MoveTo(graphTile);
            edgeEnumerator.MoveTo(vertex1);
            Assert.True(edgeEnumerator.MoveNext());
            Assert.Equal((uint) 0, edgeEnumerator.EdgeId.LocalId);
            Assert.False(edgeEnumerator.MoveNext());
        }

        [Fact]
        public void NetworkTile_Serialize_Deserialize_OneEdge_Shape()
        {
            var expected = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = expected.AddVertex(4.86638, 51.269728);
            var vertex2 = expected.AddVertex(4.86737, 51.267849);
            var edge = expected.AddEdge(vertex1, vertex2, new[]
            {
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

            var memoryStream = new MemoryStream();
            expected.WriteTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var graphTile = NetworkTile.ReadFrom(memoryStream);
            Assert.Equal((uint) 89546973, graphTile.TileId);
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude, out _));
            Assert.Equal(4.86638, longitude, 4);
            Assert.Equal(51.269728, latitude, 4);
            Assert.True(graphTile.TryGetVertex(vertex2, out longitude, out latitude, out _));
            Assert.Equal(4.86737, longitude, 4);
            Assert.Equal(51.267849, latitude, 3);

            var edgeEnumerator = new NetworkTileEnumerator();
            edgeEnumerator.MoveTo(graphTile);
            edgeEnumerator.MoveTo(vertex1);
            Assert.True(edgeEnumerator.MoveNext());
            Assert.Equal((uint) 0, edgeEnumerator.EdgeId.LocalId);
            var shapes = edgeEnumerator.Shape.ToList();
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
            Assert.False(edgeEnumerator.MoveNext());
        }

        [Fact]
        public void NetworkTile_Serialize_Deserialize_OneEdge_Shape_Elevation()
        {
            var expected = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = expected.AddVertex(4.86638, 51.269728, 100);
            var vertex2 = expected.AddVertex(4.86737, 51.267849, 110);
            var edge = expected.AddEdge(vertex1, vertex2, new[]
            {
                (
                    4.867324233055115,
                    51.269695361396586, (float?) 105
                ),
                (
                    4.867860674858093,
                    51.26909794023487, (float?) 178
                ),
                (
                    4.868037700653076,
                    51.26838639478469, (float?) 165
                )
            });

            var memoryStream = new MemoryStream();
            expected.WriteTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var graphTile = NetworkTile.ReadFrom(memoryStream);
            Assert.Equal((uint) 89546973, graphTile.TileId);
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude, out var e));
            Assert.Equal(4.86638, longitude, 4);
            Assert.Equal(51.269728, latitude, 4);
            Assert.NotNull(e);
            Assert.Equal(100, e.Value, 4);
            Assert.True(graphTile.TryGetVertex(vertex2, out longitude, out latitude, out e));
            Assert.Equal(4.86737, longitude, 4);
            Assert.Equal(51.267849, latitude, 3);
            Assert.NotNull(e);
            Assert.Equal(110, e.Value, 4);

            var edgeEnumerator = new NetworkTileEnumerator();
            edgeEnumerator.MoveTo(graphTile);
            edgeEnumerator.MoveTo(vertex1);
            Assert.True(edgeEnumerator.MoveNext());
            Assert.Equal((uint) 0, edgeEnumerator.EdgeId.LocalId);
            var shapes = edgeEnumerator.Shape.ToList();
            Assert.NotNull(shapes);
            Assert.Equal(3, shapes.Count);
            var shapePoint = shapes[0];
            Assert.Equal(4.867324233055115, shapePoint.longitude, 4);
            Assert.Equal(51.269695361396586, shapePoint.latitude, 4);
            Assert.NotNull(shapePoint.e);
            Assert.Equal(105, shapePoint.e.Value, 4);
            shapePoint = shapes[1];
            Assert.Equal(4.867860674858093, shapePoint.longitude, 4);
            Assert.Equal(51.26909794023487, shapePoint.latitude, 4);
            Assert.NotNull(shapePoint.e);
            Assert.Equal(178, shapePoint.e.Value, 4);
            shapePoint = shapes[2];
            Assert.Equal(4.868037700653076, shapePoint.longitude, 4);
            Assert.Equal(51.26838639478469, shapePoint.latitude, 4);
            Assert.NotNull(shapePoint.e);
            Assert.Equal(165, shapePoint.e.Value, 4);
            Assert.False(edgeEnumerator.MoveNext());
        }


        [Fact]
        public void NetworkTile_Serialize_Deserialize_OneEdge_Attributes()
        {
            var expected = new NetworkTile(14,
                TileStatic.ToLocalId(4.86638, 51.269728, 14));
            var vertex1 = expected.AddVertex(4.86638, 51.269728);
            var vertex2 = expected.AddVertex(4.86737, 51.267849);
            var edge = expected.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[]
                {
                    ("a_key", "A value"),
                    ("a_second_key", "Another value"),
                    ("a_last_key", "A last value")
                }
            );

            var memoryStream = new MemoryStream();
            expected.WriteTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var graphTile = NetworkTile.ReadFrom(memoryStream);
            Assert.Equal((uint) 89546973, graphTile.TileId);
            Assert.True(graphTile.TryGetVertex(vertex1, out var longitude, out var latitude, out _));
            Assert.Equal(4.86638, longitude, 4);
            Assert.Equal(51.269728, latitude, 4);
            Assert.True(graphTile.TryGetVertex(vertex2, out longitude, out latitude, out _));
            Assert.Equal(4.86737, longitude, 4);
            Assert.Equal(51.267849, latitude, 3);

            var edgeEnumerator = new NetworkTileEnumerator();
            edgeEnumerator.MoveTo(graphTile);
            edgeEnumerator.MoveTo(vertex1);
            Assert.True(edgeEnumerator.MoveNext());
            Assert.Equal((uint) 0, edgeEnumerator.EdgeId.LocalId);
            var attributes = edgeEnumerator.Attributes.ToList();
            Assert.NotNull(attributes);
            Assert.Equal(3, attributes.Count);
            Assert.Equal("a_key", attributes[0].key);
            Assert.Equal("A value", attributes[0].value);
            Assert.Equal("a_second_key", attributes[1].key);
            Assert.Equal("Another value", attributes[1].value);
            Assert.Equal("a_last_key", attributes[2].key);
            Assert.Equal("A last value", attributes[2].value);
            Assert.False(edgeEnumerator.MoveNext());
        }
    }
}