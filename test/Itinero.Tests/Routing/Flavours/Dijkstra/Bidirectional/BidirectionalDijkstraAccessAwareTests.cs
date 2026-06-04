using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Routing.Costs;
using Itinero.Routing.Flavours.Dijkstra;
using Itinero.Routing.Flavours.Dijkstra.Bidirectional;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing.Flavours.Dijkstra.Bidirectional;

/// <summary>
/// Mirror of <c>DijkstraAccessAwareTests</c> for the edge-based bidirectional Dijkstra.
/// Verifies the asymmetric per-half rule admits exactly the five valid path shapes
/// (A: all-main, B: L→N, C: N→L, D: L→N→L, E: all non-main) and rejects shape F
/// (N→L→N — the L-as-through-traffic case). Stubs <see cref="ICostFunction"/> and
/// <see cref="IsMainNFunc"/> directly so the focus stays on the search rules.
/// </summary>
public class BidirectionalDijkstraAccessAwareTests
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

    private sealed class StubCostFunction : ICostFunction
    {
        private readonly HashSet<EdgeId> _localAccessEdges;
        public StubCostFunction(HashSet<EdgeId> localAccessEdges) { _localAccessEdges = localAccessEdges; }

        public (bool canAccess, bool canStop, bool localAccess, double cost, double turnCost) Get(
            IEdgeEnumerator<RoutingNetwork> edgeEnumerator, bool tailToHead = true,
            IEnumerable<(EdgeId edgeId, byte? turn)>? previousEdges = null)
        {
            var la = _localAccessEdges.Contains(edgeEnumerator.EdgeId);
            return (true, true, la, 1.0, 0.0);
        }
    }

    private static (ICostFunction cost, IsMainNFunc isMainN) MakeStubs(
        HashSet<EdgeId> mainEdges, HashSet<EdgeId> localAccessEdges)
    {
        var cost = new StubCostFunction(localAccessEdges);
        IsMainNFunc isMainN = (edge, isLA) => isLA ? false : mainEdges.Contains(edge);
        return (cost, isMainN);
    }

    private static async Task<List<EdgeId>?> RunAsync(RouterDb db, EdgeId fromEdge, EdgeId toEdge,
        ICostFunction cost, IsMainNFunc isMainN)
    {
        var network = db.Latest;
        var source = new SnapPoint(fromEdge, 0);
        var target = new SnapPoint(toEdge, EdgeEnd);
        var (path, _) = await BidirectionalDijkstra.Default.RunAsync(network, source, target, cost,
            isMainN: isMainN);
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
        var (cost, m) = MakeStubs(new HashSet<EdgeId> { e[0], e[1], e[2] }, new HashSet<EdgeId>());

        var edges = await RunAsync(db, e[0], e[2], cost, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeB_LtoN_FindsPath()
    {
        var (db, e) = BuildChain(3);
        var (cost, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[1], e[2] },
            localAccessEdges: new HashSet<EdgeId> { e[0] });

        var edges = await RunAsync(db, e[0], e[2], cost, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeC_NtoL_FindsPath()
    {
        var (db, e) = BuildChain(3);
        var (cost, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[0], e[1] },
            localAccessEdges: new HashSet<EdgeId> { e[2] });

        var edges = await RunAsync(db, e[0], e[2], cost, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeD_LtoNtoL_FindsPath()
    {
        var (db, e) = BuildChain(4);
        var (cost, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[1], e[2] },
            localAccessEdges: new HashSet<EdgeId> { e[0], e[3] });

        var edges = await RunAsync(db, e[0], e[3], cost, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2], e[3] }, edges);
    }

    [Fact]
    public async Task ShapeE_AllNonMain_FindsPath()
    {
        var (db, e) = BuildChain(3);
        var (cost, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId>(),
            localAccessEdges: new HashSet<EdgeId> { e[0], e[1], e[2] });

        var edges = await RunAsync(db, e[0], e[2], cost, m);

        Assert.NotNull(edges);
        Assert.Equal(new[] { e[0], e[1], e[2] }, edges);
    }

    [Fact]
    public async Task ShapeF_NtoLtoN_PathBlocked()
    {
        // The structurally invalid shape — must be rejected by the per-half rule:
        // forward search starts on N, can't cross into L (would be prev_main && !curr_main).
        // Backward search starts on N, mirror-rejects from its side. Neither half reaches
        // the L portion, so no meeting → no path.
        var (db, e) = BuildChain(3);
        var (cost, m) = MakeStubs(
            mainEdges: new HashSet<EdgeId> { e[0], e[2] },
            localAccessEdges: new HashSet<EdgeId> { e[1] });

        var edges = await RunAsync(db, e[0], e[2], cost, m);

        Assert.Null(edges);
    }

    [Fact]
    public async Task NoIsMainN_NoRejection_FindsPath()
    {
        // Sanity: without IsMainNFunc the bidirectional reduces to classic. The N→L→N
        // shape that ShapeF rejects is accepted here.
        var (db, e) = BuildChain(3);
        var cost = new StubCostFunction(new HashSet<EdgeId> { e[1] });
        var network = db.Latest;

        var (path, _) = await BidirectionalDijkstra.Default.RunAsync(network,
            new SnapPoint(e[0], 0),
            new SnapPoint(e[2], EdgeEnd),
            cost);

        Assert.NotNull(path);
    }
}
