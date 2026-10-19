using System.IO;
using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles;

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
        Assert.Equal((uint)89546969, graphTile.TileId);
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
        Assert.Equal((uint)89546969, graphTile.TileId);
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
        var edge = expected.AddEdge(vertex1, vertex2, 0);

        var memoryStream = new MemoryStream();
        expected.WriteTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var graphTile = NetworkTile.ReadFrom(memoryStream);
        Assert.Equal((uint)89546973, graphTile.TileId);
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
        Assert.Equal((uint)0, edgeEnumerator.EdgeId.LocalId);
        Assert.False(edgeEnumerator.MoveNext());
    }

}
