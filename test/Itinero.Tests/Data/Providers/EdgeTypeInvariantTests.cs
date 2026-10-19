using System.Collections.Generic;
using System.Linq;
using Itinero.IO.Osm;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.Data.Providers;

/// <summary>
/// The two properties the single cost function rests on: a factor is a function of the edge type,
/// and every edge has one.
/// </summary>
public class EdgeTypeInvariantTests
{
    private static OsmDataProvider Build()
    {
        var provider = new OsmDataProvider(new OsmEnumerableStreamSource(Ways()));
        provider.Load();

        return provider;
    }

    [Fact]
    public void EveryEdge_ShouldHaveAnEdgeTypeId()
    {
        var network = Build().RouterDb.Latest;

        var seen = 0;
        var enumerator = network.GetEdgeEnumerator();
        foreach (var tileId in Tiles(network))
        {
            if (!enumerator.MoveTo(new VertexId(tileId, 0))) continue;
            while (enumerator.MoveNext())
            {
                Assert.True(enumerator.EdgeTypeId >= 0);
                seen++;
            }
        }

        Assert.True(seen > 0, "no edges were examined");
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void FactorFromEdgeType_ShouldMatchFactorFromAttributes(Profile profile)
    {
        var provider = Build();
        var network = provider.RouterDb.Latest;

        var seen = 0;
        var enumerator = network.GetEdgeEnumerator();
        foreach (var tileId in Tiles(network))
        {
            if (!enumerator.MoveTo(new VertexId(tileId, 0))) continue;
            while (enumerator.MoveNext())
            {
                var fromAttributes = profile.Factor(enumerator.Attributes);
                var fromEdgeType = profile.Factor(provider.RouterDb.EdgeTypeIndex.GetById(enumerator.EdgeTypeId));

                Assert.Equal(fromAttributes.ForwardFactor, fromEdgeType.ForwardFactor);
                Assert.Equal(fromAttributes.BackwardFactor, fromEdgeType.BackwardFactor);
                Assert.Equal(fromAttributes.ForwardSpeed, fromEdgeType.ForwardSpeed);
                Assert.Equal(fromAttributes.BackwardSpeed, fromEdgeType.BackwardSpeed);
                Assert.Equal(fromAttributes.CanStop, fromEdgeType.CanStop);
                seen++;
            }
        }

        Assert.True(seen > 0, "no edges were examined");
    }

    public static IEnumerable<object[]> Profiles =>
    [
        [OsmProfiles.Car], [OsmProfiles.Bicycle], [OsmProfiles.Pedestrian]
    ];

    private static IEnumerable<uint> Tiles(RoutingNetwork network)
    {
        var vertices = network.GetVertexEnumerator();
        var tiles = new HashSet<uint>();
        while (vertices.MoveNext()) tiles.Add(vertices.Current.TileId);

        return tiles;
    }

    // a handful of ways whose tags exercise the map: kept, dropped, and a mix.
    private static OsmGeo[] Ways()
    {
        var osm = new List<OsmGeo>();
        for (var n = 1; n <= 8; n++)
        {
            osm.Add(new Node
            {
                Id = n, Version = 1,
                Longitude = 4.800 + (n * 0.001), Latitude = 51.270 + (n * 0.0005)
            });
        }

        osm.Add(new Way
        {
            Id = 1, Version = 1, Nodes = [1L, 2, 3],
            Tags = new TagsCollection(new Tag("highway", "residential"), new Tag("name", "Main Street"))
        });
        osm.Add(new Way
        {
            Id = 2, Version = 1, Nodes = [3L, 4, 5],
            Tags = new TagsCollection(new Tag("highway", "footway"), new Tag("surface", "asphalt"))
        });
        osm.Add(new Way
        {
            Id = 3, Version = 1, Nodes = [5L, 6],
            Tags = new TagsCollection(new Tag("highway", "primary"), new Tag("maxspeed", "70"),
                new Tag("oneway", "yes"), new Tag("ref", "N1"))
        });
        osm.Add(new Way
        {
            Id = 4, Version = 1, Nodes = [6L, 7, 8],
            Tags = new TagsCollection(new Tag("highway", "cycleway"), new Tag("operator", "x"))
        });

        return osm.ToArray();
    }
}
