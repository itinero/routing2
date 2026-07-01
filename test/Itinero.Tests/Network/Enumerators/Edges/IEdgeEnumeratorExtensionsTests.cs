using System.Collections.Generic;
using System.Linq;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Xunit;

namespace Itinero.Tests.Network.Enumerators.Edges;

public class IEdgeEnumeratorExtensionsTests
{
    private static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildSingleShapelessEdge() =>
        RouterDbScaffolding.BuildRouterDb(
            [
                // A north-bound edge in Brugge; well over the 1 m assertion tolerance so any
                // interpolation error is caught immediately.
                (3.216034941620879, 51.199997638662914, (float?)null),
                (3.216587611607143, 51.200105228603580, (float?)null)
            ],
            [
                (0, 1, System.Array.Empty<(double longitude, double latitude, float? e)>())
            ]);

    private static (RouterDb db, VertexId[] vertices, EdgeId[] edges) BuildSingleShapedEdge() =>
        RouterDbScaffolding.BuildRouterDb(
            [
                (4.800000000000000, 51.268000000000000, (float?)null),
                (4.801000000000000, 51.268000000000000, (float?)null)
            ],
            new (int from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[]
            {
                (0, 1, new (double longitude, double latitude, float? e)[]
                {
                    (4.800333333333333, 51.268000000000000, (float?)null),
                    (4.800666666666666, 51.268000000000000, (float?)null),
                }),
            });

    // Regression: on a shapeless edge (only tail + head vertices), calling GetShapeBetween
    // with non-trivial offsets on BOTH ends used to return [interp(offset1), HeadLocation]
    // — the code advanced `previous` past the segment right after emitting offset1, so the
    // in-line offset2 interpolation ran against (HeadLocation, HeadLocation) and yielded
    // HeadLocation. That over-reported the sliced length by exactly the (offset2 → head)
    // portion of the edge and silently broke every downstream length/quality calc.
    [Fact]
    public void IEdgeEnumeratorExtensions_GetShapeBetween_BothOffsets_ShapelessEdge_ShouldInterpolateBoth()
    {
        var (routerDb, vertices, _) = BuildSingleShapelessEdge();
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();

        // 25% → 75% of the edge. Expected geometry is exactly the 25% and 75% interpolations
        // along the straight tail→head segment.
        var shape = enumerator.GetShapeBetween(ushort.MaxValue / 4, ushort.MaxValue / 4 * 3).ToArray();

        Assert.Equal(2, shape.Length);
        var tail = enumerator.TailLocation;
        var head = enumerator.HeadLocation;
        var expected25 = Lerp(tail, head, 0.25);
        var expected75 = Lerp(tail, head, 0.75);
        ItineroAsserts.SameLocations(expected25, shape[0]);
        ItineroAsserts.SameLocations(expected75, shape[1]);
    }

    // The length of the interpolated slice must equal (offset2 − offset1) / MaxValue × edgeLength.
    // Simple sanity check that makes the quality-metric callers immune to a return to the
    // "over-report the trailing head vertex" behavior.
    [Fact]
    public void IEdgeEnumeratorExtensions_GetShapeBetween_BothOffsets_ShapelessEdge_LengthMatchesOffsetSpan()
    {
        var (routerDb, vertices, _) = BuildSingleShapelessEdge();
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();

        var edgeLength = enumerator.EdgeLength();
        const ushort o1 = ushort.MaxValue / 4;
        const ushort o2 = ushort.MaxValue / 4 * 3;
        var expected = (o2 - o1) / (double)ushort.MaxValue * edgeLength;

        var shape = enumerator.GetShapeBetween(o1, o2).ToArray();
        Assert.Equal(2, shape.Length);
        var actual = shape[0].DistanceEstimateInMeter(shape[1]);
        Assert.InRange(actual, expected - 0.1, expected + 0.1);
    }

    // Same shapeless edge but only offset1 set — should be [interp(offset1), HeadLocation].
    [Fact]
    public void IEdgeEnumeratorExtensions_GetShapeBetween_Offset1Only_ShapelessEdge_ShouldRunToHead()
    {
        var (routerDb, vertices, _) = BuildSingleShapelessEdge();
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();

        var shape = enumerator.GetShapeBetween(ushort.MaxValue / 4).ToArray();

        Assert.Equal(2, shape.Length);
        var tail = enumerator.TailLocation;
        var head = enumerator.HeadLocation;
        ItineroAsserts.SameLocations(Lerp(tail, head, 0.25), shape[0]);
        ItineroAsserts.SameLocations(head, shape[1]);
    }

    // Same shapeless edge but only offset2 set — should be [TailLocation, interp(offset2)].
    [Fact]
    public void IEdgeEnumeratorExtensions_GetShapeBetween_Offset2Only_ShapelessEdge_ShouldStartAtTail()
    {
        var (routerDb, vertices, _) = BuildSingleShapelessEdge();
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();

        var shape = enumerator.GetShapeBetween(0, ushort.MaxValue / 4 * 3).ToArray();

        Assert.Equal(2, shape.Length);
        var tail = enumerator.TailLocation;
        var head = enumerator.HeadLocation;
        ItineroAsserts.SameLocations(tail, shape[0]);
        ItineroAsserts.SameLocations(Lerp(tail, head, 0.75), shape[1]);
    }

    // With no offsets at all, GetShapeBetween must reproduce GetCompleteShape.
    [Fact]
    public void IEdgeEnumeratorExtensions_GetShapeBetween_NoOffsets_ShouldReturnCompleteShape()
    {
        var (routerDb, vertices, _) = BuildSingleShapedEdge();
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();

        var complete = enumerator.GetCompleteShape().ToArray();
        var between = enumerator.GetShapeBetween().ToArray();
        Assert.Equal(complete.Length, between.Length);
        for (var i = 0; i < complete.Length; i++)
            ItineroAsserts.SameLocations(complete[i], between[i]);
    }

    // Multi-segment sanity: both offsets on an edge with two intermediate shape points.
    // Expected geometry threads through the interior shape points that fall inside the
    // offset window, prefixed/suffixed by the two interpolated boundary points.
    [Fact]
    public void IEdgeEnumeratorExtensions_GetShapeBetween_BothOffsets_ShapedEdge_ShouldIncludeInteriorShape()
    {
        var (routerDb, vertices, _) = BuildSingleShapedEdge();
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();

        // The edge is four straight-line east-going segments of equal-ish length. Pick
        // offsets that put both interpolated endpoints outside the two interior segments
        // — 20% and 80% — so we can assert both interior shape points survive.
        const ushort o1 = (ushort)(ushort.MaxValue * 0.20);
        const ushort o2 = (ushort)(ushort.MaxValue * 0.80);
        var shape = enumerator.GetShapeBetween(o1, o2).ToArray();

        // Expected: interp(o1), shape[0], shape[1], interp(o2).
        Assert.Equal(4, shape.Length);

        // The two interior shape points must appear untouched and in traversal order.
        ItineroAsserts.SameLocations((4.800333333333333, 51.268000000000000, (float?)null), shape[1]);
        ItineroAsserts.SameLocations((4.800666666666666, 51.268000000000000, (float?)null), shape[2]);

        // And the boundary interpolations must respect (o2 − o1) / MaxValue × edgeLength.
        var edgeLength = enumerator.EdgeLength();
        var expected = (o2 - o1) / (double)ushort.MaxValue * edgeLength;
        var actual = 0.0;
        for (var i = 0; i < shape.Length - 1; i++)
            actual += shape[i].DistanceEstimateInMeter(shape[i + 1]);
        Assert.InRange(actual, expected - 0.2, expected + 0.2);
    }

    // Backward traversal: offsets are interpreted in the traversal frame; the tail vertex
    // becomes the "head" of the edge as stored. This exercises the interaction between the
    // reversed shape and the offset math.
    [Fact]
    public void IEdgeEnumeratorExtensions_GetShapeBetween_BothOffsets_ShapelessEdge_Backward_ShouldInterpolateBoth()
    {
        var (routerDb, vertices, _) = BuildSingleShapelessEdge();
        var enumerator = routerDb.Latest.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[1]);
        enumerator.MoveNext();
        Assert.False(enumerator.Forward);

        var shape = enumerator.GetShapeBetween(ushort.MaxValue / 4, ushort.MaxValue / 4 * 3).ToArray();

        Assert.Equal(2, shape.Length);
        var tail = enumerator.TailLocation; // == the original head vertex.
        var head = enumerator.HeadLocation; // == the original tail vertex.
        ItineroAsserts.SameLocations(Lerp(tail, head, 0.25), shape[0]);
        ItineroAsserts.SameLocations(Lerp(tail, head, 0.75), shape[1]);
    }

    private static (double longitude, double latitude, float? e) Lerp(
        (double longitude, double latitude, float? e) a,
        (double longitude, double latitude, float? e) b,
        double t) =>
        (a.longitude + t * (b.longitude - a.longitude),
            a.latitude + t * (b.latitude - a.latitude),
            (float?)null);

    [Fact]
    public void IEdgeEnumeratorExtensions_GetCompleteShape_NoShape_Forward_ShouldReturnVertices()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
            },
            new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, System.Array.Empty<(double longitude, double latitude, float? e)>())
            });

        var network = routerDb.Latest;

        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();
        var shape = enumerator.GetCompleteShape().ToArray();
        Assert.Equal(2, shape.Length);
        ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[0]);
        ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[1]);
    }

    [Fact]
    public void IEdgeEnumeratorExtensions_GetCompleteShape_NoShape_Backward_ShouldReturnVertices()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
            },
            new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, System.Array.Empty<(double longitude, double latitude, float? e)>())
            });

        var network = routerDb.Latest;

        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[1]);
        enumerator.MoveNext();
        var shape = enumerator.GetCompleteShape().ToArray();
        Assert.Equal(2, shape.Length);
        ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[0]);
        ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[1]);
    }

    [Fact]
    public void IEdgeEnumeratorExtensions_GetCompleteShape_WithShape_Forward_ShouldReturnVerticesAndShape()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
            },
            new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, (float?) null),
                        (4.801242649555205, 51.268816008449830, (float?) null)
                    })
            });

        var network = routerDb.Latest;

        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[0]);
        enumerator.MoveNext();
        var shape = enumerator.GetCompleteShape().ToArray();
        Assert.Equal(4, shape.Length);
        ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[0]);
        ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426, (float?)null), shape[1]);
        ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830, (float?)null), shape[2]);
        ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[3]);
    }

    [Fact]
    public void IEdgeEnumeratorExtensions_GetCompleteShape_WithShape_Backward_ShouldReturnVerticesAndShape()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
            },
            new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[] {
                        (4.800950288772583, 51.268426671236426, (float?) null),
                        (4.801242649555205, 51.268816008449830, (float?) null)
                    })
            });

        var network = routerDb.Latest;

        var enumerator = network.GetEdgeEnumerator();
        enumerator.MoveTo(vertices[1]);
        enumerator.MoveNext();
        var shape = enumerator.GetCompleteShape().ToArray();
        Assert.Equal(4, shape.Length);
        ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250, (float?)null), shape[0]);
        ItineroAsserts.SameLocations((4.801242649555205, 51.268816008449830, (float?)null), shape[1]);
        ItineroAsserts.SameLocations((4.800950288772583, 51.268426671236426, (float?)null), shape[2]);
        ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094, (float?)null), shape[3]);
    }
}
