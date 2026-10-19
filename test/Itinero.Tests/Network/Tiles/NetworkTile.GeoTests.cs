using System.Collections.Generic;
using System.Linq;
using Itinero.Network.Tiles;
using Xunit;

namespace Itinero.Tests.Network.Tiles;

public class NetworkTile_GeoTests
{
    [Fact]
    public void NetworkTile_AddVertex0_ShouldStoreCoordinates()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.7868, 51.2643, 14));
        var vertex1 =
            networkTile.AddVertex(4.7868, 51.2643); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

        Assert.True(networkTile.TryGetVertex(vertex1, out var longitude, out var latitude, out _));
        Assert.Equal(4.7868, longitude, 4);
        Assert.Equal(51.2643, latitude, 4);
    }

    [Fact]
    public void NetworkTile_AddVertex0_ShouldStoreElevation()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.7868, 51.2643, 14));
        var vertex1 =
            networkTile.AddVertex(4.7868, 51.2643, 156); // https://www.openstreetmap.org/#map=15/51.2643/4.7868

        Assert.True(networkTile.TryGetVertex(vertex1, out _, out _, out var elevation));
        Assert.Equal(156, elevation);
    }

    [Fact]
    public void NetworkTile_AddVertex1_ShouldStoreElevation()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.7868, 51.2643, 14));
        networkTile.AddVertex(4.7868, 51.2643, 156);
        var vertex2 = networkTile.AddVertex(4.7868, 51.2643, 1541);

        Assert.True(networkTile.TryGetVertex(vertex2, out _, out _, out var elevation));
        Assert.Equal(1541, elevation);
    }

    [Fact]
    public void NetworkTile_AddEdge0_ShouldStoreEdge()
    {
        var networkTile = new NetworkTile(14,
            TileStatic.ToLocalId(4.86638, 51.269728, 14));
        var vertex1 = networkTile.AddVertex(4.86638, 51.269728);
        var vertex2 = networkTile.AddVertex(4.86737, 51.267849);

        var edge = networkTile.AddEdge(vertex1, vertex2, 0);
        Assert.Equal((uint)0, edge.LocalId);
    }
}
