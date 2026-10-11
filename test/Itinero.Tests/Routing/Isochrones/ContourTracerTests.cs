using System;
using System.Threading;
using Itinero.Routing.Isochrones;
using Xunit;

namespace Itinero.Tests.Routing.Isochrones;

/// <summary>
/// Contouring a grid directly, where the search is not involved.
/// </summary>
public class ContourTracerTests
{
    [Fact]
    public void ACancelledRequest_StopsTheTracing()
    {
        // Contouring runs once per band over the whole grid, long after the search has
        // returned, so the token has to reach it or a cancelled request still pays for it.
        const int size = 64;
        var cost = new double[size * size];
        for (var i = 0; i < cost.Length; i++) cost[i] = i % 7;

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.Throws<OperationCanceledException>(() => ContourTracer.Trace(cost, size, size,
            band: 3, minLon: 4.79, minLat: 51.26, lonPerCell: 0.0001, latPerCell: 0.0001,
            denoise: 0.01, squareMetersPerSquareDegree: 0, fillHolesBelow: 0,
            cancellationToken: cancelled.Token));
    }

    [Fact]
    public void WithoutCancellation_ItTraces()
    {
        // The companion to the test above: proves the check is not simply always throwing.
        const int size = 16;
        var cost = new double[size * size];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                // A disc of low cost, so there is exactly one ring to find.
                var dx = x - size / 2.0;
                var dy = y - size / 2.0;
                cost[y * size + x] = Math.Sqrt(dx * dx + dy * dy);
            }

        var polygons = ContourTracer.Trace(cost, size, size, band: 4, minLon: 4.79,
            minLat: 51.26, lonPerCell: 0.0001, latPerCell: 0.0001, denoise: 0.01);

        Assert.Single(polygons);
        Assert.True(polygons[0].Outer.Coordinates.Count >= 3);
    }
}
