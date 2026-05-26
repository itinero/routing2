using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Snapping;

/// <summary>
/// Tests covering the snap rejection-cascade: when the geometrically closest
/// edge fails the per-profile acceptability check (profile-inaccessible or
/// island), the snap must continue to the next candidate rather than fail.
/// These paths sit in <see cref="Snapper.IsAcceptable"/> and
/// <see cref="EdgeSearch.SnapInBoxAsync"/>'s interleaved distance/acceptability
/// loop and are easy to break when restructuring the search.
/// </summary>
public class SnappingFilteringTests
{
    private static RouterDb BuildRouterDb(
        int maxIslandSize,
        Profile profile,
        (double longitude, double latitude, float? e)[] vertices,
        (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
            List<(string, string)> attributes)[] edges,
        out VertexId[] vertexIds, out EdgeId[] edgeIds)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = maxIslandSize });
        routerDb.PrepareFor(profile);

        using var writer = routerDb.GetMutableNetwork();
        vertexIds = new VertexId[vertices.Length];
        for (var v = 0; v < vertices.Length; v++)
        {
            vertexIds[v] = writer.AddVertex(vertices[v].longitude, vertices[v].latitude, vertices[v].e);
        }

        edgeIds = new EdgeId[edges.Length];
        for (var e = 0; e < edges.Length; e++)
        {
            edgeIds[e] = writer.AddEdge(vertexIds[edges[e].from], vertexIds[edges[e].to],
                edges[e].shape, edges[e].attributes);
        }

        return routerDb;
    }

    [Fact]
    public async Task Snap_ClosestEdgeInaccessibleForProfile_ReturnsNextClosest()
    {
        // Two edges close to the query point:
        // - "foot" edge ~5m away (closer)
        // - "car"  edge ~25m away (farther)
        // Snapping with a car profile must reject the foot edge in the
        // acceptability check and return the car edge.
        var profile = new DefaultProfile("car",
            getEdgeFactor: a =>
            {
                foreach (var (k, v) in a)
                {
                    if (k == "type" && v == "foot") return EdgeFactor.NoFactor;
                }
                return new EdgeFactor(1, 1, 100, 100);
            });

        var origin = (4.800, 51.27, (float?)null);
        var footEnd = origin.OffsetWithDistanceY(10);
        var carStart = origin.OffsetWithDistanceX(25);
        var carEnd = origin.OffsetWithDistanceX(25).OffsetWithDistanceY(10);

        var routerDb = BuildRouterDb(0, profile,
            new[] { origin, footEnd, carStart, carEnd },
            new (int, int, IEnumerable<(double longitude, double latitude, float? e)>?, List<(string, string)>)[]
            {
                (0, 1, null, new List<(string, string)> { ("type", "foot") }),
                (2, 3, null, new List<(string, string)> { ("type", "car") })
            }, out _, out var edgeIds);

        var queryPoint = origin.OffsetWithDistanceY(2); // ~2m from foot edge, ~25m from car edge
        var result = await routerDb.Latest.Snap(profile, x =>
        {
            x.OffsetInMeter = 100;
            x.CheckCanStopOn = false;
        }).ToAsync(queryPoint);

        Assert.False(result.IsError, result.ErrorMessage);
        Assert.Equal(edgeIds[1], result.Value.EdgeId);
    }

    [Fact]
    public async Task Snap_AllEdgesInaccessibleForProfile_Fails()
    {
        // The only edge in range is foot-only, but we snap with a car profile.
        // There is no fallback — the snap must fail.
        var profile = new DefaultProfile("car",
            getEdgeFactor: a =>
            {
                foreach (var (k, v) in a)
                {
                    if (k == "type" && v == "foot") return EdgeFactor.NoFactor;
                }
                return new EdgeFactor(1, 1, 100, 100);
            });

        var origin = (4.800, 51.27, (float?)null);
        var end = origin.OffsetWithDistanceY(10);

        var routerDb = BuildRouterDb(0, profile,
            new[] { origin, end },
            new (int, int, IEnumerable<(double longitude, double latitude, float? e)>?, List<(string, string)>)[]
            {
                (0, 1, null, new List<(string, string)> { ("type", "foot") })
            }, out _, out _);

        var result = await routerDb.Latest.Snap(profile, x =>
        {
            x.OffsetInMeter = 100;
            x.CheckCanStopOn = false;
        }).ToAsync(origin.OffsetWithDistanceY(2));

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Snap_ClosestEdgeOnIsland_ReturnsEdgeOnMainNetwork()
    {
        // Two disconnected components in the same tile:
        // - "island": a single isolated edge at origin (1m east).
        // - "main net": a triangle 50m east of origin (3 edges in a cycle,
        //   graduates to main-net at MaxIslandSize=3 via the SCC eager merge).
        // Query point sits 2m north of origin, so the island is geometrically
        // much closer than the main net. With island filtering on, the snap
        // must reject the island edge and return one from the main net.
        var profile = new DefaultProfile("car");

        var origin = (4.800, 51.27, (float?)null);
        var islandEnd = origin.OffsetWithDistanceY(2);
        var m0 = origin.OffsetWithDistanceX(50);
        var m1 = m0.OffsetWithDistanceX(15);
        var m2 = m0.OffsetWithDistanceX(15).OffsetWithDistanceY(15);

        var routerDb = BuildRouterDb(3, profile,
            new[] { origin, islandEnd, m0, m1, m2 },
            new (int, int, IEnumerable<(double longitude, double latitude, float? e)>?, List<(string, string)>)[]
            {
                (0, 1, null, new List<(string, string)>()), // island
                (2, 3, null, new List<(string, string)>()), // main 1
                (3, 4, null, new List<(string, string)>()), // main 2
                (4, 2, null, new List<(string, string)>()), // main 3 (closes triangle)
            }, out _, out var edgeIds);

        var queryPoint = origin.OffsetWithDistanceY(1);
        var result = await routerDb.Latest.Snap(profile, x =>
        {
            x.OffsetInMeter = 100;
            x.OffsetInMeterMax = 100;
            x.MaxDistance = 100;
            x.CheckCanStopOn = false;
        }).ToAsync(queryPoint);

        Assert.False(result.IsError, result.ErrorMessage);
        var islandEdge = edgeIds[0];
        var mainEdges = new[] { edgeIds[1], edgeIds[2], edgeIds[3] };
        Assert.NotEqual(islandEdge, result.Value.EdgeId);
        Assert.Contains(result.Value.EdgeId, mainEdges);
    }

    [Fact]
    public async Task Snap_OnlyIslandsInRange_FallsBackOrFails()
    {
        // Only an island in range, no main network. The current contract is
        // that this fails cleanly rather than returning the island edge — the
        // entire point of island filtering. Documenting that here so a refactor
        // that quietly returns the island instead is flagged.
        var profile = new DefaultProfile("car");

        var origin = (4.800, 51.27, (float?)null);
        var islandEnd = origin.OffsetWithDistanceY(2);

        var routerDb = BuildRouterDb(3, profile,
            new[] { origin, islandEnd },
            new (int, int, IEnumerable<(double longitude, double latitude, float? e)>?, List<(string, string)>)[]
            {
                (0, 1, null, new List<(string, string)>())
            }, out _, out _);

        var result = await routerDb.Latest.Snap(profile, x =>
        {
            x.OffsetInMeter = 100;
            x.OffsetInMeterMax = 100;
            x.MaxDistance = 100;
            x.CheckCanStopOn = false;
        }).ToAsync(origin.OffsetWithDistanceY(1));

        Assert.True(result.IsError);
    }
}
