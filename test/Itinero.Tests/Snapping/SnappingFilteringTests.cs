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
/// edge fails the per-profile acceptability check, the snap must continue to
/// the next candidate rather than fail.
/// These paths sit in <see cref="Snapper.IsAcceptable"/> and
/// <see cref="EdgeSearch.SnapInBoxAsync"/>'s interleaved distance/acceptability
/// loop and are easy to break when restructuring the search.
/// </summary>
public class SnappingFilteringTests
{
    private static RouterDb BuildRouterDb(
        Profile profile,
        (double longitude, double latitude, float? e)[] vertices,
        (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape,
            List<(string, string)> attributes)[] edges,
        out VertexId[] vertexIds, out EdgeId[] edgeIds)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration());
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

        var routerDb = BuildRouterDb(profile,
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

        var routerDb = BuildRouterDb(profile,
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


}
