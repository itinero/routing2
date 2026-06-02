using System.Collections.Generic;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Routing.Flavours.Dijkstra.EdgeBased;
using Itinero.Snapping;
using Xunit;
using EdgeBasedDijkstra = Itinero.Routing.Flavours.Dijkstra.EdgeBased.Dijkstra;

namespace Itinero.Tests.Routing.Flavours.Dijkstra.EdgeBased;

/// <summary>
/// Covers the five valid access-aware path shapes the state-bit edge-based Dijkstra must admit,
/// plus the structurally invalid shape it must reject. Each test stubs <see cref="IsMainNFunc"/>
/// directly (no classifier pass needed): main-N is the set of edges declared "main" in the test,
/// L-tagged edges are declared via the weight func's <c>localAccess</c> field. This keeps the
/// focus on the leftMain/IsMain state-machine and away from classifier bring-up.
///
/// Path shapes (N = main-N, L = non-main):
///   A: N (all main)
///   B: L → N (start non-main, enter main)
///   C: N → L (end non-main)
///   D: L → N → L (start non-main, traverse main, end non-main)
///   E: L (all non-main)
///   F: N → L → N (re-enters main after leaving — must be rejected)
/// </summary>
public class DijkstraAccessAwareTests
{
    private const ushort EdgeEnd = ushort.MaxValue;

    private static (RouterDb db, EdgeId[] edges) BuildChain(int edgeCount)
    {
        var routerDb = new RouterDb();
        var edges = new EdgeId[edgeCount];
        using (var writer = routerDb.GetMutableNetwork())
        {
            var verts = new VertexId[edgeCount + 1];
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = writer.AddVertex(4.2500 + i * 0.0001, 51.000);
            }
            for (var i = 0; i < edgeCount; i++)
            {
                edges[i] = writer.AddEdge(verts[i], verts[i + 1]);
            }
        }
        return (routerDb, edges);
    }

    private static (DijkstraWeightFunc weight, IsMainNFunc isMainN) MakeStubs(
        HashSet<EdgeId> mainEdges, HashSet<EdgeId> localAccessEdges)
    {
        DijkstraWeightFunc weight = (e, _) => (1.0, 0.0, localAccessEdges.Contains(e.EdgeId));
        IsMainNFunc isMainN = (edge, isLA) => isLA ? false : mainEdges.Contains(edge);
        return (weight, isMainN);
    }

    private static async Task<List<EdgeId>?> RunAsync(RouterDb db, EdgeId fromEdge, EdgeId toEdge,
        DijkstraWeightFunc weight, IsMainNFunc isMainN)
    {
        var network = db.Latest;
        var source = new SnapPoint(fromEdge, 0);
        var target = new SnapPoint(toEdge, EdgeEnd);
        var (path, _) = await EdgeBasedDijkstra.Default.RunAsync(network,
            (source, null), (target, null), weight, isMainN: isMainN);
        if (path == null) return null;
        var result = new List<EdgeId>();
        using var en = path.GetEnumerator();
        while (en.MoveNext()) result.Add(en.Current.edge);
        return result;
    }

    [Fact]
    public async Task ShapeA_AllMain_FindsPath()
    {
        var (db, e) = BuildChain(3);
        var (w, m) = MakeStubs(new HashSet<EdgeId> { e[0], e[1], e[2] }, new HashSet<EdgeId>());

        var edges = await RunAsync(db, e[0], e[2], w, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeB_LtoN_FindsPath()
    {
        // Origin on a non-main (L) edge, then enters main and stays main.
        var (db, e) = BuildChain(3);
        var (w, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[1], e[2] },
            localAccessEdges: new HashSet<EdgeId> { e[0] });

        var edges = await RunAsync(db, e[0], e[2], w, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeC_NtoL_FindsPath()
    {
        // Source in main, then leaves main onto the destination's L edge.
        var (db, e) = BuildChain(3);
        var (w, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[0], e[1] },
            localAccessEdges: new HashSet<EdgeId> { e[2] });

        var edges = await RunAsync(db, e[0], e[2], w, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeD_LtoNtoL_FindsPath()
    {
        // L → N → L: origin pocket, traverse mainland, end in destination pocket.
        var (db, e) = BuildChain(4);
        var (w, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[1], e[2] },
            localAccessEdges: new HashSet<EdgeId> { e[0], e[3] });

        var edges = await RunAsync(db, e[0], e[3], w, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2], e[3] }, edges);
    }

    [Fact]
    public async Task ShapeE_AllNonMain_FindsPath()
    {
        // Origin and destination in the same pocket — search never enters main.
        var (db, e) = BuildChain(3);
        var (w, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId>(),
            localAccessEdges: new HashSet<EdgeId> { e[0], e[1], e[2] });

        var edges = await RunAsync(db, e[0], e[2], w, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeF_NtoLtoN_PathBlocked()
    {
        // N → L → N is the structurally invalid shape: the L hop is used as through-traffic
        // between two main-N segments. Once the search transitions main → non-main at the L edge
        // leftMain flips true; relaxing back onto a main-N edge is then rejected, so the only
        // path is unreachable.
        var (db, e) = BuildChain(3);
        var (w, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[0], e[2] },
            localAccessEdges: new HashSet<EdgeId> { e[1] });

        var edges = await RunAsync(db, e[0], e[2], w, m);

        Assert.Null(edges);
    }

    [Fact]
    public async Task NoIsMainN_NoRejection_FindsPath()
    {
        // Sanity: without an IsMainNFunc supplied the search reduces to the classic edge-based
        // Dijkstra. The N → L → N shape that ShapeF rejects is accepted here.
        var (db, e) = BuildChain(3);
        DijkstraWeightFunc weight = (en, _) => (1.0, 0.0, en.EdgeId == e[1]);

        var network = db.Latest;
        var (path, _) = await EdgeBasedDijkstra.Default.RunAsync(network,
            (new SnapPoint(e[0], 0), null),
            (new SnapPoint(e[2], EdgeEnd), null),
            weight);

        Assert.NotNull(path);
    }
}
