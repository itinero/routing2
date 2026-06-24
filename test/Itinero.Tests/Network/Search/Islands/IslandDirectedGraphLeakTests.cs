using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Tests.Network.Search.Islands;

/// <summary>
/// Reproduces the production "snap never ends" failure at the algorithm
/// level. The Notion / in-repo island-detection spec says (Tile-based batching
/// and persistence, step 3):
///
///   <em>Once a tile is fully classified, discard the tile-local dg vertices,
///   keeping only the MainNet sentinel and whatever links existed before the
///   tile started. The dg never accumulates per-tile edge ids.</em>
///
/// In the current implementation that discard step is missing. Every edge
/// from every tile classified during the process's lifetime stays in
/// <see cref="IslandDirectedGraph"/>'s <c>_parent</c> / <c>_outgoing</c> /
/// <c>_incoming</c> / <c>_members</c> dictionaries. Once those grow large,
/// <see cref="IslandDirectedGraph.AddDirectedLink"/>'s eager-cycle-merge
/// (<c>PathExistsNoLock</c> + <c>IntersectReachableNoLock</c>) does an
/// O(V+E) BFS over the entire dg per call — eventually the next snap stalls.
///
/// This test classifies N tiles back-to-back and reads the Full-kind dg's
/// vertex count after each. The spec demands the count stays bounded by the
/// number of cross-tile links plus the sentinel. If the count grows linearly
/// with N, the leak is reproduced.
/// </summary>
public class IslandDirectedGraphLeakTests
{
    private readonly ITestOutputHelper _output;

    public IslandDirectedGraphLeakTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DgVerticesAreNotDiscardedAfterTileDone()
    {
        // MaxIslandSize=2 keeps things small: every isolated 2-edge component
        // graduates immediately, so the dg shouldn't need to hold anything
        // tile-local across tiles — yet the test will show it does.
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 2 });
        var profile = new DefaultProfile();

        // Build many disjoint 2-edge components scattered across a large
        // longitude range; group by actual TileId after the fact and pick N
        // tiles that each contain a complete component. Avoids the "tile
        // boundary fell between v1 and v2" gotcha that plagues hand-picked
        // coords.
        const int targetTiles = 50;
        const double startLon = 4.0;
        const double lat = 51.0;
        const double lonStep = 0.05;     // well over one z14 tile (~0.022°)
        const double vertOff = 0.00001;  // ~0.7m: stays inside any tile

        var componentsByTile = new Dictionary<uint, List<EdgeId>>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            for (var i = 0; componentsByTile.Count < targetTiles && i < targetTiles * 3; i++)
            {
                var lon = startLon + i * lonStep;
                var v1 = writer.AddVertex(lon, lat);
                var v2 = writer.AddVertex(lon + vertOff, lat);
                var v3 = writer.AddVertex(lon + 2 * vertOff, lat);
                var e1 = writer.AddEdge(v1, v2);
                var e2 = writer.AddEdge(v2, v3);
                // Skip components that straddle a tile boundary — keeps the
                // "one component per tile" invariant simple.
                if (e1.TileId != e2.TileId) continue;
                if (componentsByTile.ContainsKey(e1.TileId)) continue;
                componentsByTile[e1.TileId] = new List<EdgeId> { e1, e2 };
            }
        }

        Assert.True(componentsByTile.Count >= 20,
            $"only built {componentsByTile.Count} clean components; expected >= 20");
        var tilesById = componentsByTile.Keys.ToList();
        var tileCount = tilesById.Count;

        var network = routerDb.Latest;
        var dgFull = network.IslandManager.GetOrCreateDirectedGraph(profile, IslandKind.Full);

        var perTileCounts = new List<int>(tileCount);
        for (var i = 0; i < tileCount; i++)
        {
            await IslandClassifier.BuildForTileAsync(
                network, profile, tilesById[i], CancellationToken.None);

            // GetAllEdges excludes the MainNet sentinel. Per the spec this
            // count should stay at a small bound (sentinel + cross-tile links
            // only). With the leak it grows linearly with tile count.
            var count = dgFull.GetAllEdges().Count;
            perTileCounts.Add(count);
            _output.WriteLine($"after tile {i + 1}/{tileCount}: dg.GetAllEdges().Count = {count}");
        }

        // The spec allows a small bounded number of vertices to persist
        // across tiles (cross-tile links to the sentinel). 8 is generous —
        // these are disjoint components so legitimate cross-tile state is 0.
        // If the bound is breached, the leak is reproduced; the per-tile log
        // above shows the growth pattern.
        var final = perTileCounts[^1];
        Assert.True(final <= 8,
            $"dg vertex count grew to {final} after {tileCount} disjoint tiles — " +
            "the tile-local discard step from the spec is missing; the dg is leaking.");
    }
}
