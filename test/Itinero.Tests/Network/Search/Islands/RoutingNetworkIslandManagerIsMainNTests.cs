using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

/// <summary>
/// Tests for the <see cref="RoutingNetworkIslandManager.IsMainN"/> lookup. The
/// API resolution order is:
/// L-tag → <see cref="Islands"/> → <see cref="Locals"/> → done-tile default → null.
/// </summary>
public class RoutingNetworkIslandManagerIsMainNTests
{
    [Fact]
    public void IsMainN_LocalAccessEdge_ShouldReturnFalse_WithoutTouchingStorage()
    {
        // L-tagged edges are never main-N. The lookup short-circuits before
        // any storage is consulted — verified here by asking about a tile
        // whose state is completely empty.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;
        var unrelatedEdge = new EdgeId(0, 0);

        var result = manager.IsMainN(profile, unrelatedEdge, isLocalAccess: true);

        Assert.Equal(false, result);
    }

    [Fact]
    public void IsMainN_NoState_ShouldReturnNull()
    {
        // Nothing classified yet → the manager has no verdict.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;
        var edge = new EdgeId(0, 0);

        var result = manager.IsMainN(profile, edge, isLocalAccess: false);

        Assert.Null(result);
    }

    [Fact]
    public void IsMainN_EdgeInIslandsSet_ShouldReturnFalse()
    {
        // Unreachable in Full → in Islands set → not main-N.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;
        var edge = new EdgeId(0, 0);

        var islands = manager.GetIslandsFor(profile);
        islands.SetEdgeOnIsland(edge);

        var result = manager.IsMainN(profile, edge, isLocalAccess: false);

        Assert.Equal(false, result);
    }

    [Fact]
    public void IsMainN_EdgeMarkedLocal_ShouldReturnFalse()
    {
        // Non-main-N pocket → marked local on Islands → not main-N.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;
        var edge = new EdgeId(0, 0);

        var islands = manager.GetIslandsFor(profile);
        islands.SetEdgeLocal(edge);

        var result = manager.IsMainN(profile, edge, isLocalAccess: false);

        Assert.Equal(false, result);
    }

    [Fact]
    public void IsMainN_DoneTile_EdgeNotInEitherSet_ShouldReturnTrue()
    {
        // Done tile + not L-tagged + not in Islands + not in Locals → main-N
        // by elimination, the default we never store explicitly.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;
        var edge = new EdgeId(0, 0);

        var islands = manager.GetIslandsFor(profile);
        islands.SetTileDone(edge.TileId);

        var result = manager.IsMainN(profile, edge, isLocalAccess: false);

        Assert.Equal(true, result);
    }

    [Fact]
    public void IsMainN_TileNotDone_EdgeNotInEitherSet_ShouldReturnNull()
    {
        // Without a done tile we can't claim main-N by default — the verdict
        // is genuinely unknown.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;
        var edge = new EdgeId(0, 0);

        // Touch the islands set to make sure the dict has an entry (without
        // setting tile-done). This exercises the path where the profile has
        // state but the tile itself isn't finalised.
        manager.GetIslandsFor(profile);

        var result = manager.IsMainN(profile, edge, isLocalAccess: false);

        Assert.Null(result);
    }

    [Fact]
    public void GetOrCreateDirectedGraph_DifferentKinds_ShouldReturnSeparateInstances()
    {
        // Each (profile, kind) pair must have its own DG — sharing would
        // mean the NonLocal classification corrupts Full state and vice versa.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;

        var fullDg = manager.GetOrCreateDirectedGraph(profile, IslandKind.Full);
        var nonLocalDg = manager.GetOrCreateDirectedGraph(profile, IslandKind.NonLocal);

        Assert.NotSame(fullDg, nonLocalDg);
    }

    [Fact]
    public void GetOrCreateDirectedGraph_SameKindCalledTwice_ShouldReturnSameInstance()
    {
        // The DG is shared across classification runs within the same kind —
        // that's how the oracle pattern accumulates state.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;

        var first = manager.GetOrCreateDirectedGraph(profile, IslandKind.Full);
        var second = manager.GetOrCreateDirectedGraph(profile, IslandKind.Full);

        Assert.Same(first, second);
    }

    [Fact]
    public void IsMainN_LocalAccessTakesPrecedenceOverIslandsMembership()
    {
        // L-tag is the first check — even if storage would have said something
        // different, isLocalAccess=true wins.
        var routerDb = new RouterDb();
        var profile = new DefaultProfile();
        routerDb.PrepareFor(profile);
        var manager = routerDb.Latest.IslandManager;
        var edge = new EdgeId(0, 0);

        var islands = manager.GetIslandsFor(profile);
        islands.SetTileDone(edge.TileId);
        // If the lookup didn't short-circuit on L-tag, the done-tile fall-
        // through would return true here. With L-tag short-circuit it returns
        // false.

        var result = manager.IsMainN(profile, edge, isLocalAccess: true);

        Assert.Equal(false, result);
    }
}
