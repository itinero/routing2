using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles;

public class NetworkTile_AttributesTests
{
    [Fact]
    public void NetworkTile_AddEdge0_OneAttribute_ShouldStoreAttribute()
    {
        var graphTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
        var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

        var edge = graphTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[] {
                    ("a_key", "A value")
                }
        );

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(graphTile);
        Assert.True(enumerator.MoveTo(edge, true));
        var attributes = enumerator.Attributes;
        Assert.NotNull(attributes);
        Assert.Single(attributes);
        var attribute = attributes.First();
        Assert.Equal("a_key", attribute.key);
        Assert.Equal("A value", attribute.value);
    }

    [Fact]
    public void NetworkTile_AddEdge0_ThreeAttributes_ShouldStoreAttributes()
    {
        var graphTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = graphTile.AddVertex(4.86638, 51.269728);
        var vertex2 = graphTile.AddVertex(4.86737, 51.267849);

        var edge = graphTile.AddEdge(vertex1, vertex2, attributes: new (string key, string value)[] {
                    ("a_key", "A value"),
                    ("a_second_key", "Another value"),
                    ("a_last_key", "A last value")
                }
        );

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(graphTile);
        Assert.True(enumerator.MoveTo(edge, true));
        var attributes = enumerator.Attributes.ToList();
        Assert.NotNull(attributes);
        Assert.Equal(3, attributes.Count);
        Assert.Equal("a_key", attributes[0].key);
        Assert.Equal("A value", attributes[0].value);
        Assert.Equal("a_second_key", attributes[1].key);
        Assert.Equal("Another value", attributes[1].value);
        Assert.Equal("a_last_key", attributes[2].key);
        Assert.Equal("A last value", attributes[2].value);
    }
}
