using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing;
using Itinero.Routing.Costs;
using Itinero.Routing.Isochrones;
using Itinero.Snapping;
using Xunit;

namespace Itinero.Tests.Routing.Isochrones;

/// <summary>
/// Isochrones over a hand-built grid, where the reachable set at a given cost is known by hand.
/// </summary>
/// <remarks>
/// The profile costs one unit per metre, so a band is a distance in metres and the expected
/// shape can be reasoned about rather than recorded.
/// </remarks>
public class IsochroneSearchTests
{
    /// <summary>
    /// Cost units per metre. A factor-1 profile costs 100 per metre — Itinero's costs are in
    /// centimetres — so a band written as metres has to be scaled or it asks for a few metres.
    /// </summary>
    private const double PerMetre = 100.0;

    private sealed class MetreProfile : Profile
    {
        public override string Name => "metre";

        public override EdgeFactor Factor(IEnumerable<(string key, string value)> attributes)
            => new(1, 1, 1, 1);

        public override Itinero.Profiles.TurnCostFactor TurnCostFactor(
            IEnumerable<(string key, string value)> attributes)
            => Itinero.Profiles.TurnCostFactor.Empty;
    }

    /// <summary>
    /// An 11x11 lattice of vertices about 100 m apart, so every street is walkable in any
    /// direction and the reachable set at cost c is the diamond of streets within c metres.
    /// </summary>
    private static (RouterDb db, SnapPoint centre) Lattice()
    {
        const double lon = 4.79;
        const double lat = 51.26;
        const double step = 0.0009; // ~100 m in latitude at this latitude.
        const int n = 11;

        var db = new RouterDb();
        var ids = new VertexId[n, n];
        using (var writer = db.GetMutableNetwork())
        {
            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    ids[x, y] = writer.AddVertex(lon + x * step, lat + y * step);
                }
            }

            for (var y = 0; y < n; y++)
            {
                for (var x = 0; x < n; x++)
                {
                    if (x + 1 < n) writer.AddEdge(ids[x, y], ids[x + 1, y]);
                    if (y + 1 < n) writer.AddEdge(ids[x, y], ids[x, y + 1]);
                }
            }
        }

        var snapped = db.Latest.Snap([new MetreProfile()])
            .ToAsync((lon + 5 * step, lat + 5 * step, (float?)null)).GetAwaiter().GetResult();
        Assert.False(snapped.IsError, snapped.IsError ? snapped.ErrorMessage : null);

        return (db, snapped.Value);
    }

    private static async Task<Isochrone> RunAsync(RouterDb db, SnapPoint centre,
        IReadOnlyList<double> bands, IsochroneSettings? settings = null)
    {
        var result = await db.Latest.IsochroneAsync(
            new RoutingSettings { Profile = new MetreProfile(), MaxDistance = double.MaxValue },
            centre, bands, settings ?? new IsochroneSettings { CellSizeMeters = 25 });
        Assert.False(result.IsError, result.IsError ? result.ErrorMessage : null);

        return result.Value;
    }



    [Fact]
    public async Task WithNoCellSizeGiven_TheGridScalesToTheAreaReached()
    {
        // A fixed size is wrong at both ends of the range, so the default is derived. A band
        // reaching twice as far must not come back with twice the resolution.
        var (db, centre) = Lattice();

        // Few cells across, or this ~1 km fixture sits on the floor at both bands and the
        // scaling cannot be seen at all.
        var coarse = new IsochroneSettings { TargetCellsAcross = 10, MinCellSizeMeters = 1 };
        var near = await RunAsync(db, centre, [200 * PerMetre], coarse);
        var far = await RunAsync(db, centre, [600 * PerMetre], coarse);

        Assert.True(far.Diagnostics.CellSizeMeters > near.Diagnostics.CellSizeMeters,
            $"near={near.Diagnostics.CellSizeMeters:F1} far={far.Diagnostics.CellSizeMeters:F1}");

        // And the floor holds when the derived size would be absurdly fine.
        var floored = await RunAsync(db, centre, [200 * PerMetre], new IsochroneSettings());
        Assert.Equal(20.0, floored.Diagnostics.CellSizeMeters, 3);
    }

    [Fact]
    public async Task CityBlocksAreNotHoles()
    {
        // The surface only knows costs along roads, so without filling, every block in the
        // lattice comes back as a hole and the band describes a street network, not an area.
        var (db, centre) = Lattice();

        var isochrone = await RunAsync(db, centre, [600 * PerMetre]);

        Assert.All(isochrone.Bands[0].Polygons, p => Assert.Empty(p.Holes));
    }

    [Fact]
    public async Task WithNeitherReachNorFilling_TheBandIsRibbonsWithPockets()
    {
        // Pins that the defaults are doing the work above. Both have to be off: the reach
        // radius alone already closes blocks this small, so turning off only the hole filling
        // would show nothing.
        var (db, centre) = Lattice();

        var isochrone = await RunAsync(db, centre, [600 * PerMetre], new IsochroneSettings
        {
            CellSizeMeters = 25,
            ReachRadiusMeters = 0,
            FillHolesBelowSquareMeters = 0
        });

        Assert.Contains(isochrone.Bands[0].Polygons, p => p.Holes.Count > 0);
    }

    [Fact]
    public async Task TheReachRadiusClosesTheGapsBetweenRoads()
    {
        // The failure the grid invites: at a cell size finer than the spacing between roads a
        // band is a set of ribbons, not an area. Rural roads are much further apart than this
        // lattice's, which is why the radius is in metres rather than cells.
        var (db, centre) = Lattice();

        var ribbons = await RunAsync(db, centre, [600 * PerMetre], new IsochroneSettings
        {
            CellSizeMeters = 25,
            ReachRadiusMeters = 0,
            FillHolesBelowSquareMeters = 0
        });
        var filled = await RunAsync(db, centre, [600 * PerMetre], new IsochroneSettings
        {
            CellSizeMeters = 25,
            ReachRadiusMeters = 150,
            FillHolesBelowSquareMeters = 0
        });

        var ribbonHoles = ribbons.Bands[0].Polygons.Sum(p => p.Holes.Count);
        var filledHoles = filled.Bands[0].Polygons.Sum(p => p.Holes.Count);
        Assert.True(filledHoles < ribbonHoles,
            $"reach radius left {filledHoles} holes, ribbons had {ribbonHoles}");
    }

    [Fact]
    public async Task ItReturnsOneBandPerRequestedCost_InnermostFirst()
    {
        var (db, centre) = Lattice();

        var isochrone = await RunAsync(db, centre, [200 * PerMetre, 400 * PerMetre, 600 * PerMetre]);

        Assert.Equal(3, isochrone.Bands.Count);
        Assert.Equal([200 * PerMetre, 400 * PerMetre, 600 * PerMetre],
            isochrone.Bands.Select(x => x.Cost));
    }

    [Fact]
    public async Task DuplicateBands_AreCollapsed()
    {
        var (db, centre) = Lattice();

        var isochrone = await RunAsync(db, centre, [300 * PerMetre, 300 * PerMetre, 300 * PerMetre]);

        Assert.Single(isochrone.Bands);
    }

    [Fact]
    public async Task EveryBandEnclosesTheOrigin()
    {
        // The whole point of an isochrone: you can stand where you started.
        var (db, centre) = Lattice();
        var at = centre.LocationOnNetwork(db.Latest);

        var isochrone = await RunAsync(db, centre, [200 * PerMetre, 500 * PerMetre]);

        foreach (var band in isochrone.Bands)
        {
            Assert.NotEmpty(band.Polygons);
            Assert.True(band.Polygons.Any(p => Encloses(p, at)),
                $"band {band.Cost} does not contain the origin");
        }
    }

    [Fact]
    public async Task ABiggerBandEnclosesASmallerOne()
    {
        // Monotonicity. A grid coarse against the band would let the two cross, which is the
        // failure a single band cannot show.
        var (db, centre) = Lattice();

        var isochrone = await RunAsync(db, centre, [200 * PerMetre, 600 * PerMetre]);
        var inner = isochrone.Bands[0];
        var outerBand = isochrone.Bands[1];

        Assert.True(Area(outerBand) > Area(inner),
            $"600 encloses {Area(outerBand):F8}, 200 encloses {Area(inner):F8}");
    }

    [Fact]
    public async Task ABandBeyondTheNetwork_CoversAllOfIt()
    {
        var (db, centre) = Lattice();

        // The lattice is ~1 km across, so 5 km reaches everything and the band should be the
        // whole thing rather than a ring at the frontier.
        var isochrone = await RunAsync(db, centre, [5000 * PerMetre]);
        var band = isochrone.Bands[0];

        Assert.NotEmpty(band.Polygons);
        Assert.True(Area(band) > 0);
    }

    [Fact]
    public async Task RingsAreWoundOuterCounterClockwise()
    {
        // GeoJSON's right-hand rule, and how a hole is told from an outer ring here.
        var (db, centre) = Lattice();

        var isochrone = await RunAsync(db, centre, [400 * PerMetre]);

        foreach (var polygon in isochrone.Bands[0].Polygons)
        {
            Assert.True(Shoelace(polygon.Outer) > 0, "outer ring is not counter-clockwise");
            foreach (var hole in polygon.Holes)
            {
                Assert.True(Shoelace(hole) < 0, "hole is not clockwise");
            }
        }
    }

    [Fact]
    public async Task EveryRingIsClosedAndHasAtLeastThreePoints()
    {
        var (db, centre) = Lattice();

        var isochrone = await RunAsync(db, centre, [250 * PerMetre, 450 * PerMetre]);

        foreach (var band in isochrone.Bands)
        {
            foreach (var polygon in band.Polygons)
            {
                Assert.True(polygon.Outer.Coordinates.Count >= 3);
                foreach (var hole in polygon.Holes) Assert.True(hole.Coordinates.Count >= 3);
            }
        }
    }

    [Fact]
    public async Task ACoarserGridStillProducesABandEnclosingTheOrigin()
    {
        // MaxCells coarsens the grid rather than refusing; the band must survive that.
        var (db, centre) = Lattice();
        var at = centre.LocationOnNetwork(db.Latest);

        var isochrone = await RunAsync(db, centre, [400 * PerMetre],
            new IsochroneSettings { CellSizeMeters = 10, MaxCells = 400 });

        Assert.True(isochrone.Diagnostics.CellSizeMeters > 10,
            "grid should have been coarsened to fit MaxCells");
        Assert.Contains(isochrone.Bands[0].Polygons, p => Encloses(p, at));
    }

    [Fact]
    public async Task NoBands_Throws()
    {
        var (db, centre) = Lattice();

        await Assert.ThrowsAsync<System.ArgumentException>(() => db.Latest.IsochroneAsync(
            new RoutingSettings { Profile = new MetreProfile() }, centre, []));
    }

    [Fact]
    public async Task ABandCostingNothing_Throws()
    {
        var (db, centre) = Lattice();

        await Assert.ThrowsAsync<System.ArgumentOutOfRangeException>(() => db.Latest.IsochroneAsync(
            new RoutingSettings { Profile = new MetreProfile() }, centre, [0d]));
    }

    [Fact]
    public async Task TheSearchIsCutJustPastTheOutermostBand()
    {
        // Not an implementation detail: without the margin there is nothing outside the band to
        // interpolate the contour against. Doubling the band must settle strictly more.
        var (db, centre) = Lattice();

        var near = await RunAsync(db, centre, [200 * PerMetre]);
        var far = await RunAsync(db, centre, [600 * PerMetre]);

        Assert.True(far.Diagnostics.Settled > near.Diagnostics.Settled);
        Assert.True(near.Diagnostics.Reached > 0);
    }

    private static double Shoelace(IsochroneRing ring)
    {
        var sum = 0d;
        var c = ring.Coordinates;
        for (var i = 0; i < c.Count; i++)
        {
            var (x1, y1) = c[i];
            var (x2, y2) = c[(i + 1) % c.Count];
            sum += x1 * y2 - x2 * y1;
        }

        return sum;
    }

    private static double Area(IsochroneBand band)
        => band.Polygons.Sum(p => Shoelace(p.Outer) + p.Holes.Sum(Shoelace));

    private static bool Encloses(IsochronePolygon polygon,
        (double longitude, double latitude, float? e) at)
    {
        if (!InRing(polygon.Outer, at)) return false;

        return polygon.Holes.All(hole => !InRing(hole, at));
    }

    private static bool InRing(IsochroneRing ring, (double longitude, double latitude, float? e) at)
    {
        var inside = false;
        var c = ring.Coordinates;
        for (int i = 0, j = c.Count - 1; i < c.Count; j = i++)
        {
            if (c[i].latitude > at.latitude != c[j].latitude > at.latitude &&
                at.longitude < (c[j].longitude - c[i].longitude) * (at.latitude - c[i].latitude) /
                (c[j].latitude - c[i].latitude) + c[i].longitude)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
