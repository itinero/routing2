using System.Linq;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Network.TurnCosts;

/// Turn cost orders are packed into 4 bits per edge, so a vertex can carry at most 15 of
/// them. Real data exceeds that: on the AP-6 toll plaza (OSM node 2151903449) 26 ways end
/// on one node, and a restriction there used to throw out of OrderCoder and take the whole
/// tile build down with it.
public class TurnCostOrderBudgetTests
{
    private static (RouterDb routerDb, VertexId centre, EdgeId[] edges) Star(int spokes)
    {
        var routerDb = new RouterDb();
        VertexId centre;
        var edges = new EdgeId[spokes];
        using (var mutable = routerDb.GetMutableNetwork())
        {
            centre = mutable.AddVertex(4.792613983154297, 51.26535213392538, (float?)null);
            for (var i = 0; i < spokes; i++)
            {
                var outer = mutable.AddVertex(
                    4.792613983154297 + ((i + 1) * 0.0001), 51.26535213392538, (float?)null);
                edges[i] = mutable.AddEdge(centre, outer);
            }
        }

        return (routerDb, centre, edges);
    }

    [Fact]
    public void AddTurnCosts_WithinOrderBudget_IsAdded()
    {
        var (routerDb, centre, edges) = Star(4);

        using var mutable = routerDb.GetMutableNetwork();
        var added = mutable.AddTurnCosts(centre, Enumerable.Empty<(string key, string value)>(),
            new[] { edges[0], edges[1] }, new uint[,] { { 0, 1 }, { 0, 0 } });

        Assert.True(added);
    }

    [Fact]
    public void AddTurnCosts_ExceedingOrderBudgetInOneCall_IsSkippedNotThrown()
    {
        var (routerDb, centre, edges) = Star(20);

        using var mutable = routerDb.GetMutableNetwork();
        var costs = new uint[20, 20];
        var added = mutable.AddTurnCosts(centre,
            Enumerable.Empty<(string key, string value)>(), edges, costs);

        Assert.False(added);
    }

    [Fact]
    public void AddTurnCosts_ExceedingOrderBudgetAcrossCalls_IsSkippedNotThrown()
    {
        // The toll plaza shape: each call is small enough to pass any per-call check, but
        // orders accumulate per vertex, so the budget runs out partway through.
        var (routerDb, centre, edges) = Star(20);

        using var mutable = routerDb.GetMutableNetwork();
        var results = new bool[edges.Length - 1];
        for (var i = 0; i < edges.Length - 1; i++)
        {
            results[i] = mutable.AddTurnCosts(centre,
                Enumerable.Empty<(string key, string value)>(),
                new[] { edges[i], edges[i + 1] }, new uint[,] { { 0, 1 }, { 0, 0 } });
        }

        Assert.Contains(true, results);
        Assert.Contains(false, results);
    }

    [Fact]
    public void AddTurnCosts_SkippedCall_LeavesEarlierTurnCostsIntact()
    {
        // A skip must not half-write: whatever fit before it still has to read back.
        var (routerDb, centre, edges) = Star(20);

        using (var mutable = routerDb.GetMutableNetwork())
        {
            Assert.True(mutable.AddTurnCosts(centre,
                Enumerable.Empty<(string key, string value)>(),
                new[] { edges[0], edges[1] }, new uint[,] { { 0, 1 }, { 0, 0 } }));

            var costs = new uint[20, 20];
            Assert.False(mutable.AddTurnCosts(centre,
                Enumerable.Empty<(string key, string value)>(), edges, costs));
        }

        // centre is the tail of every spoke, so the order assigned there is the tail order.
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(edges[0], true);
        Assert.NotNull(enumerator.TailOrder);

        enumerator.MoveTo(edges[1], true);
        Assert.NotNull(enumerator.TailOrder);

        // and nothing beyond the two that fit was given an order.
        for (var i = 2; i < edges.Length; i++)
        {
            enumerator.MoveTo(edges[i], true);
            Assert.Null(enumerator.TailOrder);
        }
    }
}
