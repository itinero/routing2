using System;
using System.Linq;
using Itinero.Network;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles;

public class NetworkTileTests
{
    [Fact]
    public void NetworkTile_AddVertex_TileEmpty_ShouldReturn0()
    {
        // when adding a vertex to a tile the graph should always generate an id in the same tile.

        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.7868, 51.2643, 14));
        var vertex1 =
            networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
        Assert.Equal((uint)89546969, vertex1.TileId);
        Assert.Equal((uint)0, vertex1.LocalId);
    }

    [Fact]
    public void NetworkTile_AddVertex_OneVertex_ShouldReturn1()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.7868, 51.2643, 14));
        var vertex1 =
            networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
        Assert.Equal((uint)89546969, vertex1.TileId);
        Assert.Equal((uint)0, vertex1.LocalId);

        // when adding the vertex a second time it should generate a new local id.

        vertex1 = networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868
        Assert.Equal((uint)89546969, vertex1.TileId);
        Assert.Equal((uint)1, vertex1.LocalId);
    }

    [Fact]
    public void NetworkTile_TryGetVertex0_Vertex0DoesNotExists_ShouldReturnFalse()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.7868, 51.2643, 14));

        Assert.False(networkTile.TryGetVertex(new VertexId(networkTile.TileId, 0),
            out var longitude, out var latitude, out _));
    }

    [Fact]
    public void NetworkTile_AddEdge0_VerticesExist_ShouldReturn0()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
        var vertex2 = networkTile.AddVertex(4.86737, 51.267849);

        var edge = networkTile.AddEdge(vertex1, vertex2, 0);
        Assert.Equal((uint)0, edge.LocalId);
    }

    [Fact]
    public void NetworkTile_AddEdge1_VerticesExist_ShouldReturn7()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
        var vertex2 = networkTile.AddVertex(4.86737, 51.267849);

        // the first edge takes 7 bytes: two vertices, the type id, the length and the orders.
        networkTile.AddEdge(vertex1, vertex2, 0);
        // the second edge get the pointer as id.
        var edge = networkTile.AddEdge(vertex2, vertex1, 0);
        Assert.Equal((uint)7, edge.LocalId);
    }

    [Fact]
    public void NetworkTile_AddEdge_OverTileBoundary_ShouldStoreVertex()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
        var vertex2 = new VertexId(vertex1.TileId + 1, 451);

        var edge = networkTile.AddEdge(vertex1, vertex2, 0);
        Assert.Equal((uint)0 + EdgeId.MinCrossId, edge.LocalId);

        var edgePointer = networkTile.GetEdgeCrossPointer(edge.LocalId - EdgeId.MinCrossId);
        var size = networkTile.DecodeVertex(edgePointer,
            out var localId, out var tileId);
        Assert.Equal((uint)0, localId);
        Assert.Equal((uint)vertex1.TileId, tileId);
        size = networkTile.DecodeVertex(edgePointer + size, out localId, out tileId);
        Assert.Equal((uint)451, localId);
        Assert.Equal((uint)vertex1.TileId + 1, tileId);
    }

    [Fact]
    public void NetworkTile_AddEdge_ShouldAddEdge()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
        var vertex2 = networkTile.AddVertex(4.86737, 51.267849);
        var edge = networkTile.AddEdge(vertex1, vertex2, 0);

        var enumerator = new NetworkTileEnumerator();
        enumerator.MoveTo(networkTile);
        enumerator.MoveTo(vertex1);
        enumerator.MoveNext();
        ItineroAsserts.Equal(enumerator, new EdgeId(networkTile.TileId, 0),
            vertex1, vertex2);
    }





}
