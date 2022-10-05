using System.Linq;
using Itinero.IO.Osm.Restrictions;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions.Turns;

public class OsmTurnRestrictionParserTests
{
    [Fact]
    public void OsmTurnRestrictionParser_ToNetworkRestriction_Positive_ViaNode_ShouldReturnPositiveRestriction()
    {
        var osmGeos = new OsmGeo[]
        {
            new Node()
            {
                Id = 1,
                Version = 1,
                Longitude = 4.80177104473114,
                Latitude = 51.26888649155825
            },
            new Node()
            {
                Id = 2,
                Version = 1,
                Longitude = 4.788472652435303,
                Latitude = 51.2667216038964,
            },
            new Node()
            {
                Id = 3,
                Version = 1,
                Longitude = 4.8004889488220215,
                Latitude = 51.268856284525015
            },
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        };

        var relation = new Relation()
        {
            Id = 1,
            Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way), new RelationMember(2, "via", OsmGeoType.Node),
                new RelationMember(2, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "no_right_turn"))
        };

        var parser = new OsmTurnRestrictionParser();
        var result = parser.TryParse(relation,
            k => { return osmGeos.First(x => x.Id == k && x.Type == OsmGeoType.Way) as Way; },
            out var osmTurnRestriction);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        Assert.NotNull(osmTurnRestriction);
        Assert.True(osmTurnRestriction.IsProbibitory);
        Assert.Equal(1, osmTurnRestriction.From.FirstOrDefault()?.Id);
        Assert.Equal(2, osmTurnRestriction.To.FirstOrDefault()?.Id);
        Assert.Equal(2, osmTurnRestriction.ViaNodeId);
        Assert.Empty(osmTurnRestriction.Via);
    }

    [Fact]
    public void OsmTurnRestrictionParser_ToNetworkRestriction_Positive_ViaWay_ShouldReturnPositiveRestriction()
    {
        var osmGeos = new OsmGeo[]
        {
            new Node()
            {
                Id = 1,
                Version = 1,
                Longitude = 4.80177104473114,
                Latitude = 51.26888649155825
            },
            new Node()
            {
                Id = 2,
                Version = 1,
                Longitude = 4.788472652435303,
                Latitude = 51.2667216038964,
            },
            new Node()
            {
                Id = 3,
                Version = 1,
                Longitude = 4.8004889488220215,
                Latitude = 51.268856284525015
            },
            new Node()
            {
                Id = 4,
                Version = 1,
                Longitude = 4.8004889488220215,
                Latitude = 51.268856284525015
            },
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
            new Way()
            {
                Id = 3,
                Version = 1,
                Nodes = new[] { 3L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            },
        };

        var relation = new Relation()
        {
            Id = 1,
            Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way),
                new RelationMember(2, "via", OsmGeoType.Way),
                new RelationMember(3, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "no_right_turn"))
        };

        var parser = new OsmTurnRestrictionParser();
        var result = parser.TryParse(relation,
            k => { return osmGeos.First(x => x.Id == k && x.Type == OsmGeoType.Way) as Way; },
            out var osmTurnRestriction);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        Assert.NotNull(osmTurnRestriction);
        Assert.True(osmTurnRestriction.IsProbibitory);
        Assert.Equal(1, osmTurnRestriction.From.FirstOrDefault()?.Id);
        Assert.Equal(2, osmTurnRestriction.Via.FirstOrDefault()?.Id);
        Assert.Equal(3, osmTurnRestriction.To.FirstOrDefault()?.Id);
        Assert.Null(osmTurnRestriction.ViaNodeId);
    }
}