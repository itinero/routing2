using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Data;
using Itinero.IO.Osm;
using Itinero.IO.Osm.Tiles;
using Itinero.Network;
using Itinero.Network.Tiles;
using Itinero.Network.Tiles.Standalone;
using Itinero.Network.Tiles.Standalone.Writer;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.Network.Tiles.Standalone.Writer;

/// <summary>
/// Inserting tiles concurrently must produce the same network as inserting them one at a time.
/// </summary>
/// <remarks>
/// The rest of the merge tests are single-threaded, so they say nothing about the locking that
/// makes concurrent insertion safe. This is the test that does, and it is the gate for that work:
/// a data race here corrupts the network silently, and throughput numbers from a corrupted
/// network look perfectly healthy.
///
/// Equivalence is checked on geometry rather than on ids. Cross-tile edge ids come from a
/// per-tile counter (<c>_nextCrossTileId</c>), so which boundary edge gets which id genuinely
/// depends on the order tiles arrive — that difference is legitimate and comparing ids would
/// report it as a failure.
///
/// Races are probabilistic, so each case runs repeatedly. A single pass proves very little.
/// </remarks>
public class ConcurrentTileInsertTests
{
    /// <remarks>
    /// Races are probabilistic and this is the only test that can catch them, so it buys coverage
    /// with repetition rather than with one careful pass. The corridor spans enough tiles that
    /// Parallel.ForEach actually runs several inserts at once; a short corridor finishes before
    /// any interleaving happens and proves nothing.
    /// </remarks>
    private const int Repeats = 25;

    /// <summary>
    /// A way running east across many zoom-14 tiles, with bollards to force restrictions.
    /// </summary>
    /// <remarks>
    /// Crossing many boundaries is the point: boundary crossings are the only case where one
    /// insert writes two tiles, so they are where a lock-ordering mistake would deadlock and
    /// where a lost claim would drop or duplicate an edge.
    /// </remarks>
    private static (OsmGeo[] osm, List<(uint x, uint y)> tiles) BuildCorridor()
    {
        const double lat = 51.269;
        var lons = new List<double>();
        for (var i = 0; i < 300; i++)
        {
            lons.Add(4.90 + (i * 0.0025));
        }

        var nodes = new OsmGeo[lons.Count];
        for (var i = 0; i < lons.Count; i++)
        {
            nodes[i] = new Node { Id = i + 1, Longitude = lons[i], Latitude = lat };
            // a bollard every 7th node, so restrictions land in many different tiles
            if (i > 0 && i % 7 == 0)
            {
                ((Node)nodes[i]).Tags = new TagsCollection(new Tag("barrier", "bollard"));
            }
        }

        var way = new Way
        {
            Id = 100,
            Nodes = Enumerable.Range(1, lons.Count).Select(i => (long)i).ToArray(),
            Tags = new TagsCollection(new Tag("highway", "residential"))
        };

        var osm = new List<OsmGeo>(nodes) { way }.ToArray();

        var tiles = new List<(uint x, uint y)>();
        foreach (var lon in lons)
        {
            var t = TileStatic.WorldToTile(lon, lat, 14);
            if (!tiles.Contains(t)) tiles.Add(t);
        }

        return (osm, tiles);
    }

    private static RouterDb NewRouterDb() => new(new RouterDbConfiguration
    {
        Zoom = 14,
        EdgeTypeMap = new OsmEdgeTypeMap()
    });

    private static List<StandaloneNetworkTile> BuildTiles(RouterDb routerDb,
        IEnumerable<(uint x, uint y)> tiles, OsmGeo[] osm)
    {
        var built = new List<StandaloneNetworkTile>();
        foreach (var (x, y) in tiles)
        {
            var w = routerDb.Latest.GetStandaloneTileWriter(x, y);
            w.AddTileData(osm);
            built.Add(w.GetResultingTile());
        }

        return built;
    }

    /// <summary>
    /// Every edge as "endpoint|endpoint|length", endpoints ordered so direction does not matter.
    /// </summary>
    /// <remarks>
    /// A set, not a list: a cross-tile edge is stored in both of its tiles and is reached from
    /// both of its ends, so the same edge is encountered several times by construction. Ordering
    /// the endpoints and de-duplicating is what makes the comparison independent of how the
    /// network was assembled.
    /// </remarks>
    private static List<string> CanonicalEdges(RoutingNetwork network)
    {
        var edges = new HashSet<string>();
        foreach (var vertex in network.GetVertices())
        {
            var enumerator = network.GetEdgeEnumerator();
            if (!enumerator.MoveTo(vertex)) continue;
            while (enumerator.MoveNext())
            {
                if (!network.TryGetVertex(enumerator.Tail, out var tLon, out var tLat, out _)) continue;
                if (!network.TryGetVertex(enumerator.Head, out var hLon, out var hLat, out _)) continue;

                var a = FormattableString.Invariant($"{tLon:F6},{tLat:F6}");
                var b = FormattableString.Invariant($"{hLon:F6},{hLat:F6}");
                var ends = string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
                edges.Add(FormattableString.Invariant($"{ends}|{enumerator.Length}"));
            }
        }

        var list = edges.ToList();
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    private static int CountTurnCostVertices(RoutingNetwork network)
    {
        var count = 0;
        foreach (var vertex in network.GetVertices())
        {
            var enumerator = network.GetEdgeEnumerator();
            if (!enumerator.MoveTo(vertex)) continue;
            while (enumerator.MoveNext())
            {
                if (enumerator.TailOrder == null) continue;
                count++;
                break;
            }
        }

        return count;
    }

    [Fact]
    public void ConcurrentInsert_ManyTilesWithBoundaryCrossings_MatchesSerialInsert()
    {
        var (osm, tiles) = BuildCorridor();
        Assert.True(tiles.Count >= 4,
            $"expected the corridor to span several tiles, got {tiles.Count}");

        // Serial reference, built once.
        var serialDb = NewRouterDb();
        var serialTiles = BuildTiles(serialDb, tiles, osm);
        var serialIds = new GlobalNetworkManager();
        foreach (var t in serialTiles)
        {
            using var writer = serialDb.Latest.GetWriter();
            writer.AddStandaloneTile(t, serialIds);
        }

        var expected = CanonicalEdges(serialDb.Latest);
        var expectedTurnCostVertices = CountTurnCostVertices(serialDb.Latest);
        Assert.NotEmpty(expected);

        for (var repeat = 0; repeat < Repeats; repeat++)
        {
            var db = NewRouterDb();
            var built = BuildTiles(db, tiles, osm);
            var ids = new GlobalNetworkManager();

            Parallel.ForEach(built, t =>
            {
                using var writer = db.Latest.GetTileInsertWriter();
                writer.AddStandaloneTile(t, ids);
            });

            Assert.Equal(expected, CanonicalEdges(db.Latest));
            Assert.Equal(expectedTurnCostVertices, CountTurnCostVertices(db.Latest));
            Assert.Equal(serialIds.PendingBoundaryCrossingCount, ids.PendingBoundaryCrossingCount);
            Assert.Equal(serialIds.PendingRestrictionCount, ids.PendingRestrictionCount);
        }
    }

    [Fact]
    public void ConcurrentInsert_TwoNeighboursFromOppositeSides_DoesNotDeadlock()
    {
        // The specific shape that hangs if lock ordering is wrong: two tiles that share a
        // boundary, inserted at the same moment, each needing the other's lock to build the
        // crossing edge. Ordering "my tile, then my partner" deadlocks here — A takes 100 then
        // wants 101 while B takes 101 then wants 100. Ordering by stripe makes both take the same
        // lock first, so one simply waits.
        //
        // Asserts on time, because the failure is a hang and not a wrong answer. Many short
        // rounds rather than one long one: the collision has to be hit, and each round restarts
        // both threads together.
        var (osm, tiles) = BuildCorridor();
        var pairs = new List<(uint x, uint y)>[] { };

        var task = Task.Run(() =>
        {
            for (var round = 0; round < 60; round++)
            {
                var db = NewRouterDb();
                // Two adjacent tiles only, so both threads contend on the same boundary.
                var adjacent = tiles.Take(2).ToList();
                var built = BuildTiles(db, adjacent, osm);
                var ids = new GlobalNetworkManager();

                // Opposite orders, started together.
                var forward = Task.Run(() =>
                {
                    using var w = db.Latest.GetTileInsertWriter();
                    w.AddStandaloneTile(built[0], ids);
                });
                var backward = Task.Run(() =>
                {
                    using var w = db.Latest.GetTileInsertWriter();
                    w.AddStandaloneTile(built[1], ids);
                });
                Task.WaitAll(forward, backward);
            }
        });

        Assert.True(task.Wait(TimeSpan.FromSeconds(60)),
            "two neighbours inserted from opposite sides did not finish - lock ordering is not a " +
            "total order, so each is holding the lock the other needs");
        Assert.True(task.IsCompletedSuccessfully, task.Exception?.ToString() ?? "faulted");
    }

    [Fact]
    public void ConcurrentInsert_SameTilesRepeatedly_DoesNotDeadlock()
    {
        // The shape a lock-ordering mistake fails on: two inserts that share a boundary, started
        // together, each holding one end. Ordering the two tile locks by id is what prevents it,
        // and the failure mode is a hang rather than a wrong answer — so this asserts on time.
        var (osm, tiles) = BuildCorridor();

        var task = Task.Run(() =>
        {
            for (var repeat = 0; repeat < Repeats; repeat++)
            {
                var db = NewRouterDb();
                var built = BuildTiles(db, tiles, osm);
                var ids = new GlobalNetworkManager();

                // Reversed halves started against each other, to maximise the chance that two
                // threads reach the same boundary from opposite sides at the same moment.
                var forward = built.Take(built.Count / 2).ToList();
                var backward = built.Skip(built.Count / 2).Reverse().ToList();
                Parallel.ForEach(forward.Concat(backward), t =>
                {
                    using var writer = db.Latest.GetTileInsertWriter();
                    writer.AddStandaloneTile(t, ids);
                });
            }
        });

        Assert.True(task.Wait(TimeSpan.FromSeconds(60)),
            "concurrent tile insertion did not finish - suspect a lock-ordering deadlock between " +
            "two tiles sharing a boundary");
        Assert.True(task.IsCompletedSuccessfully, task.Exception?.ToString() ?? "faulted");
    }
}
