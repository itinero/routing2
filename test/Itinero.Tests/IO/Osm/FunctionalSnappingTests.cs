using System.Linq;
using System.Threading.Tasks;
using Itinero.IO.Osm;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Routing;
using Itinero.Snapping;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm;

public class FunctionalSnappingTests
{
    private static RouterDb CreateRouterDb(OsmGeo[] os, Profile? profile = null)
    {
        var routerDb = new RouterDb(new RouterDbConfiguration { MaxIslandSize = 0 });
        if (profile != null) routerDb.PrepareFor(profile);
        routerDb.UseOsmData(new OsmEnumerableStreamSource(os), s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });
        return routerDb;
    }

    private static OsmGeo[] SingleResidentialWay()
    {
        return new OsmGeo[]
        {
            new Node { Id = 1, Longitude = 4.800, Latitude = 51.270 },
            new Node { Id = 2, Longitude = 4.801, Latitude = 51.269 },
            new Way
            {
                Id = 1, Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        };
    }

    [Fact]
    public async Task Snap_WithoutProfile_ShouldWork()
    {
        var routerDb = CreateRouterDb(SingleResidentialWay());
        var network = routerDb.Latest;

        var snap = await network.Snap().ToAsync(4.800, 51.270);
        Assert.False(snap.IsError, snap.ErrorMessage);
    }

    [Fact]
    public async Task Snap_WithCarProfile_ShouldWork()
    {
        var profile = OsmProfiles.Car;
        var routerDb = CreateRouterDb(SingleResidentialWay(), profile);
        var network = routerDb.Latest;

        var snap = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap.IsError, snap.ErrorMessage);
    }

    [Fact]
    public async Task Snap_WithCarProfile_BothEnds_ShouldSnapToSameEdge()
    {
        var profile = OsmProfiles.Car;
        var routerDb = CreateRouterDb(SingleResidentialWay(), profile);
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);

        var snap2 = await network.Snap(profile).ToAsync(4.801, 51.269);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        Assert.Equal(snap1.Value.EdgeId, snap2.Value.EdgeId);
    }

    [Fact]
    public async Task Snap_WithCarProfile_FullRouteFlow()
    {
        var profile = OsmProfiles.Car;
        var routerDb = CreateRouterDb(SingleResidentialWay(), profile);
        var network = routerDb.Latest;

        var snap1 = await network.Snap(profile).ToAsync(4.800, 51.270);
        Assert.False(snap1.IsError, snap1.ErrorMessage);
        var snap2 = await network.Snap(profile).ToAsync(4.801, 51.269);
        Assert.False(snap2.IsError, snap2.ErrorMessage);

        var route = await network.Route(profile)
            .From(snap1.Value)
            .To(snap2.Value)
            .CalculateAsync();

        Assert.False(route.IsError, route.ErrorMessage);
        Assert.NotNull(route.Value);
        Assert.True(route.Value.Shape.Count >= 2);
    }
}
