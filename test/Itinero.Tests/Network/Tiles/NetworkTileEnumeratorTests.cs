using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles;

public class NetworkTile_EnumeratorTests
{
    [Fact]
    public void NetworkTileEnumerator_OneVertex_MoveToVertex_ShouldReturnTrue()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex = networkTile.AddVertex(4.86638, 51.269728, (float?)null);

        var enumerator = new NetworkTileEnumerator();
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

        var edge = networkTile.AddEdge(vertex1, vertex2, 0);

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(networkTile);
        enumerator.MoveTo(vertex1);
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.Forward);
        Assert.Equal(vertex1, enumerator.Tail);
        Assert.Equal(vertex2, enumerator.Head);
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

        var edge = networkTile.AddEdge(vertex1, vertex2, 0);

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(networkTile);
        enumerator.MoveTo(vertex2);
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.Forward);
        Assert.Equal(vertex2, enumerator.Tail);
        Assert.Equal(vertex1, enumerator.Head);
        Assert.Equal(edge, enumerator.EdgeId);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void NetworkTileEnumerator_OneEdge_MoveToEdgeForward_ShouldMoveTo_EdgeForward()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = networkTile.AddVertex(4.86638, 51.269728, (float?)null);
        var vertex2 = networkTile.AddVertex(4.86737, 51.267849, (float?)null);

        var edge = networkTile.AddEdge(vertex1, vertex2, 0);

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(networkTile);
        Assert.True(enumerator.MoveTo(edge, true));
        Assert.True(enumerator.Forward);
        Assert.Equal(vertex1, enumerator.Tail);
        Assert.Equal(vertex2, enumerator.Head);
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

        var edge = networkTile.AddEdge(vertex1, vertex2, 0);

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(networkTile);
        Assert.True(enumerator.MoveTo(edge, false));
        Assert.False(enumerator.Forward);
        Assert.Equal(vertex2, enumerator.Tail);
        Assert.Equal(vertex1, enumerator.Head);
        Assert.Equal(edge, enumerator.EdgeId);
        Assert.False(enumerator.MoveNext());
    }
}
