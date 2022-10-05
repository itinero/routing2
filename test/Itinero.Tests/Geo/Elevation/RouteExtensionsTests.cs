using System.Collections.Generic;
using Itinero.Geo.Elevation;
using Itinero.Routes;
using Xunit;

namespace Itinero.Tests.Geo.Elevation;

public class RouteExtensionsTests
{
    [Fact]
    public void RouteExtensions_AddElevation_1ShapePoint_WithoutHandler_ShouldNotAddElevation()
    {
        var route = new Route
        {
            Shape = new List<(double longitude, double latitude, float? e)> {
                    (1, 2, null)
                }
        };

        route.AddElevation();

        Assert.Null(route.Shape[0].e);
    }

    [Fact]
    public void RouteExtensions_AddElevation_1ShapePoint_WithHandler_ShouldAddElevation()
    {
        var route = new Route
        {
            Shape = new List<(double longitude, double latitude, float? e)> {
                    (1, 2, null)
                }
        };

        var elevationHandler = new ElevationHandler((lon, lat) => (float)(lon + lat));

        route.AddElevation(elevationHandler);

        Assert.NotNull(route.Shape[0].e);
        Assert.Equal(3, route.Shape[0].e.Value, 1);
    }
}
