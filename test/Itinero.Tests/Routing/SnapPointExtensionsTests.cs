using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Snapping;
using Itinero.Tests.Mocks.Costs;
using Xunit;

namespace Itinero.Tests.Routing;

public class SnapPointExtensionsTests
{
    [Fact]
    public void SnapPointExtensions_TrySingleHopPath_Identical_ShouldReturnTrueAndZero()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            [
                (3.1074142456054688, 51.31012070202407, null),
                (3.1095707416534424, 51.31076453560284, null)
            ],
            [
                (0, 1, null)
            ]);

        var routingNetwork = routerDb.Latest;
        var origin = new SnapPoint(edges[0], 15365);

        var costFunction = MockCostFunction.Create(0, 1);

        Assert.True(routingNetwork.TrySingleHop(origin, origin, costFunction, out var path, out var cost));
        Assert.Equal(0, cost);
        Assert.Single(path);
        Assert.Equal(edges[0], path[0].edge);
        Assert.Equal(origin.Offset, path.Offset1);
        Assert.Equal(origin.Offset, path.Offset2);
    }

    [Fact]
    public void SnapPointExtensions_TrySingleHopPath_ForwardPossible_ShouldReturnTrueAndSingleHop()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            [
                (3.1074142456054688, 51.31012070202407, null),
                (3.1095707416534424, 51.31076453560284, null)
            ],
            [
                (0, 1, null)
            ]);

        var routingNetwork = routerDb.Latest;
        var origin = new SnapPoint(edges[0], 0);
        var destination = new SnapPoint(edges[0], ushort.MaxValue);

        var costFunction = MockCostFunction.Create(0, 1);

        Assert.True(routingNetwork.TrySingleHop(origin, destination, costFunction, out var path, out var cost));
        Assert.Equal(1, cost);
        Assert.Single(path);
        Assert.Equal(edges[0], path[0].edge);
        Assert.Equal(0, path.Offset1);
        Assert.Equal(ushort.MaxValue, path.Offset2);
    }

    [Fact]
    public void SnapPointExtensions_TrySingleHopPath_ForwardImpossible_ShouldReturnFalse()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            [
                (3.1074142456054688, 51.31012070202407, null),
                (3.1095707416534424, 51.31076453560284, null)
            ],
            [
                (0, 1, null)
            ]);

        var routingNetwork = routerDb.Latest;
        var origin = new SnapPoint(edges[0], 0);
        var destination = new SnapPoint(edges[0], ushort.MaxValue);

        var costFunction = MockCostFunction.Create(1, 0);

        Assert.False(routingNetwork.TrySingleHop(origin, destination, costFunction, out _, out _));
    }

    [Fact]
    public void SnapPointExtensions_TrySingleHopPath_NotSameEdge_ShouldReturnFalse()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            [
                (3.1074142456054688, 51.31012070202407, null),
                (3.1095707416534424, 51.31076453560284, null)
            ],
            [
                (0, 1, null),
                (1, 0, null)
            ]);

        var routingNetwork = routerDb.Latest;
        var origin = new SnapPoint(edges[0], 0);
        var destination = new SnapPoint(edges[1], ushort.MaxValue);

        var costFunction = MockCostFunction.Create(0);

        Assert.False(routingNetwork.TrySingleHop(origin, destination, costFunction, out _, out _));
    }

    [Fact]
    public void SnapPointExtensions_TrySingleHopPath_OffsetsImpossible_ShouldReturnFalse()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            [
                (3.1074142456054688, 51.31012070202407, null),
                (3.1095707416534424, 51.31076453560284, null)
            ],
            [
                (0, 1, null)
            ]);

        var routingNetwork = routerDb.Latest;
        var origin = new SnapPoint(edges[0], 13000);
        var destination = new SnapPoint(edges[0], 12000);

        var costFunction = MockCostFunction.Create(0, 1);

        Assert.False(routingNetwork.TrySingleHop(origin, destination, costFunction, out _, out _));
    }

    [Fact]
    public void SnapPointExtensions_TrySingleHopPath_OnlyBackwardPossible_OffsetsDecrease_ShouldReturnTrueAndSingleHop()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            [
                (3.1074142456054688, 51.31012070202407, null),
                (3.1095707416534424, 51.31076453560284, null)
            ],
            [
                (0, 1, null)
            ]);

        var routingNetwork = routerDb.Latest;
        var offset2 = (ushort)(ushort.MaxValue / 4);
        var offset1 = (ushort)(offset2 + offset2);
        var origin = new SnapPoint(edges[0], offset1);
        var destination = new SnapPoint(edges[0], offset2);

        var costFunction = MockCostFunction.Create(1, 0);

        Assert.True(routingNetwork.TrySingleHop(origin, destination, costFunction, out var path, out var cost));
        Assert.Equal(0.25, cost, 2);
        Assert.Single(path);
        Assert.Equal(edges[0], path[0].edge);
        Assert.False(path[0].forward);
        Assert.Equal(ushort.MaxValue - offset1, path.Offset1);
        Assert.Equal(ushort.MaxValue - offset2, path.Offset2);
    }
}
