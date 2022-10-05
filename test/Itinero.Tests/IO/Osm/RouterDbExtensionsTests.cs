using Itinero.IO.Osm;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm;

public class RouterDbExtensionsTests
{
    [Fact]
    public void RouterDbExtensions_AllData_ShouldIgnoreNothing()
    {
        var os = new OsmGeo[] {
            new Node() {
                Id = 0,
                Longitude = 4.789937138557434,
                Latitude = 51.267201580037316
            },
            new Node() {
                Id = 1,
                Longitude = 4.789524078369141,
                Latitude = 51.265409195988916
            },
            new Way() {
                Id = 2,
                Nodes = new []{ 0L, 1 }
            }
        };

        var routerDb = new RouterDb();
        // include all the data.
        routerDb.UseOsmData(new OsmEnumerableStreamSource(os), s =>
        {
            s.TagsFilter.Filter = null;
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });

        var vertices = routerDb.Latest.GetVertexEnumerator();
        Assert.True(vertices.MoveNext());
        Assert.True(vertices.MoveNext());
        Assert.False(vertices.MoveNext());
    }

    [Fact]
    public void RouterDbExtensions_OnlyHighways_ShouldTakeOnlyHighway()
    {
        var os = new OsmGeo[] {
            new Node() {
                Id = 0,
                Longitude = 4.789937138557434,
                Latitude = 51.267201580037316
            },
            new Node() {
                Id = 1,
                Longitude = 4.789524078369141,
                Latitude = 51.265409195988916
            },
            new Node() {
                Id = 2,
                Longitude = 4.789937138557434,
                Latitude = 51.267201580037316
            },
            new Node() {
                Id = 3,
                Longitude = 4.789524078369141,
                Latitude = 51.265409195988916
            },
            new Way() {
                Id = 0,
                Nodes = new []{ 0L, 1 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way() {
                Id = 1,
                Nodes = new []{ 2L, 3 },
                Tags = new TagsCollection(new Tag("not-a-highway", "residential"))
            }
        };

        var routerDb = new RouterDb();
        // include all the data.
        routerDb.UseOsmData(new OsmEnumerableStreamSource(os), s =>
        {
            s.TagsFilter.Filter = o =>
            {
                if (o.Type == OsmGeoType.Node) return true;
                if (o.Tags == null) return false;
                if (!o.Tags.ContainsKey("highway")) return false;

                return true;
            };
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });

        var vertices = routerDb.Latest.GetVertexEnumerator();
        var edges = routerDb.Latest.GetEdgeEnumerator();
        Assert.True(vertices.MoveNext());
        Assert.True(edges.MoveTo(vertices.Current));
        Assert.True(edges.MoveNext());
        Assert.Equal(new[] { ("highway", "residential") }, edges.Attributes);
        Assert.False(edges.MoveNext());
        Assert.True(vertices.MoveNext());
        Assert.False(vertices.MoveNext());
    }

    [Fact]
    public void RouterDbExtensions_OnlyHighways_DefaultFilters_ShouldTakeOnlyHighway()
    {
        var os = new OsmGeo[] {
            new Node() {
                Id = 0,
                Longitude = 4.789937138557434,
                Latitude = 51.267201580037316
            },
            new Node() {
                Id = 1,
                Longitude = 4.789524078369141,
                Latitude = 51.265409195988916
            },
            new Node() {
                Id = 2,
                Longitude = 4.789937138557434,
                Latitude = 51.267201580037316
            },
            new Node() {
                Id = 3,
                Longitude = 4.789524078369141,
                Latitude = 51.265409195988916
            },
            new Way() {
                Id = 0,
                Nodes = new []{ 0L, 1 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way() {
                Id = 1,
                Nodes = new []{ 2L, 3 },
                Tags = new TagsCollection(new Tag("not-a-highway", "residential"))
            }
        };

        var routerDb = new RouterDb();
        // include all the data.
        routerDb.UseOsmData(new OsmEnumerableStreamSource(os), s =>
        {
            s.TagsFilter.Filter = o =>
            {
                if (o.Type == OsmGeoType.Node) return true;
                if (o.Tags == null) return false;
                if (!o.Tags.ContainsKey("highway")) return false;

                return true;
            };
            s.TagsFilter.CompleteFilter = null;
            s.TagsFilter.MemberFilter = null;
        });

        var vertices = routerDb.Latest.GetVertexEnumerator();
        var edges = routerDb.Latest.GetEdgeEnumerator();
        Assert.True(vertices.MoveNext());
        Assert.True(edges.MoveTo(vertices.Current));
        Assert.True(edges.MoveNext());
        Assert.Equal(new[] { ("highway", "residential") }, edges.Attributes);
        Assert.False(edges.MoveNext());
        Assert.True(vertices.MoveNext());
        Assert.False(vertices.MoveNext());
    }
}
