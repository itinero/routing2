using System.Collections.Generic;
using System.Linq;
using Itinero.Geo;
using NetTopologySuite.Geometries;
using Xunit;

namespace Itinero.Tests.Geo;

public class AsStreamTest
{
    [Fact]
    public void RouterDb_AsStream_AllFeaturesAreEnumerated()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null)
            },
            new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0])
            });

        var stream = routerDb.AsStream().ToList();
        Assert.NotEmpty(stream);
        Assert.Equal(3, stream.Count);
        var ls = stream[1].Geometry as LineString;
        Assert.NotNull(ls);
        var c0 = ls.Coordinates[0];
        var c1 = ls.Coordinates[1];
        ItineroAsserts.SameLocations((4.801073670387268, 51.268064181900094), (c0.X, c0.Y));
        ItineroAsserts.SameLocations((4.801771044731140, 51.268886491558250), (c1.X, c1.Y));
    }


    [Fact]
    public void RouterDb2edges_AsStream_AllFeaturesAreEnumerated()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            new (double longitude, double latitude, float? e)[] {
                    (4.801073670387268, 51.268064181900094, (float?) null),
                    (4.801771044731140, 51.268886491558250, (float?) null),
                    (4.801775044731140, 51.26835136491558250, (float?) null)
            },
            new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e)[0]),
                    (2, 1, new (double longitude, double latitude, float? e)[0])
            });

        var stream = routerDb.AsStream().ToList();
        Assert.NotEmpty(stream);
        Assert.Equal(5, stream.Count);
    }

    [Fact]
    public void RouterDbMultiEdge_AsStream_AllCoordinatesAreEnumerated()
    {
        var (routerDb, vertices, edges) = RouterDbScaffolding.BuildRouterDb(
            new (double longitude, double latitude, float? e)[] {
                    (4.0, 51.0, (float?) null),
                    (8, 52, (float?) null),
            },
            new (int @from, int to, IEnumerable<(double longitude, double latitude, float? e)>? shape)[] {
                    (0, 1, new (double longitude, double latitude, float? e) [] {
                        (5, 51.0, (float?) null),
                        (6, 51.0, (float?) null),
                        (7, 51.0, (float?) null),
                    }),
            });

        var stream = routerDb.AsStream().ToList();
        Assert.NotEmpty(stream);
        Assert.Equal(3, stream.Count);
        var ls = stream[1].Geometry as LineString;
        Assert.NotNull(ls);
        var coordinates = ls.Coordinates;
        Assert.Equal(5, coordinates.Length);
        Assert.Equal(4.0, coordinates[4].X);
        Assert.Equal(5, coordinates[3].X);
        Assert.Equal(6, coordinates[2].X);
        Assert.Equal(7, coordinates[1].X);
        Assert.Equal(8, coordinates[0].X);
    }


    [Fact]
    public void EmptyRouterDb_AsStream_AllFeaturesAreEnumerated()
    {
        var routerDb = new RouterDb();

        var stream = routerDb.AsStream().ToList();
        Assert.Empty(stream);
    }
}
