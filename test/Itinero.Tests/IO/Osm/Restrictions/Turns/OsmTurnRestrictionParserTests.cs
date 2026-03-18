using System;
using System.Linq;
using Itinero.IO.Osm.Restrictions.Turns;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions.Turns;

public class OsmTurnRestrictionParserTests
{
    [Fact]
    public void IsRestriction_NoRightTurn_ShouldReturnTrueAndProhibitory()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Tags = new TagsCollection(
                new Tag("type", "restriction"),
                new Tag("restriction", "no_right_turn"))
        };

        var result = parser.IsRestriction(relation, out var isProhibitive);

        Assert.True(result);
        Assert.True(isProhibitive);
    }

    [Fact]
    public void IsRestriction_OnlyStraightOn_ShouldReturnTrueAndNotProhibitory()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Tags = new TagsCollection(
                new Tag("type", "restriction"),
                new Tag("restriction", "only_straight_on"))
        };

        var result = parser.IsRestriction(relation, out var isProhibitive);

        Assert.True(result);
        Assert.False(isProhibitive);
    }

    [Fact]
    public void IsRestriction_NonRestrictionRelation_ShouldReturnFalse()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Tags = new TagsCollection(
                new Tag("type", "multipolygon"))
        };

        var result = parser.IsRestriction(relation, out _);

        Assert.False(result);
    }

    [Fact]
    public void IsRestriction_NullTags_ShouldReturnFalse()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation { Id = 1, Version = 1 };

        var result = parser.IsRestriction(relation, out _);

        Assert.False(result);
    }

    [Fact]
    public void IsRestriction_VehicleSpecificRestriction_ShouldReturnTrue()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Tags = new TagsCollection(
                new Tag("type", "restriction"),
                new Tag("restriction:bicycle", "no_left_turn"))
        };

        var result = parser.IsRestriction(relation, out var isProhibitive);

        Assert.True(result);
        Assert.True(isProhibitive);
    }

    [Fact]
    public void IsRestriction_UnknownRestrictionType_ShouldReturnFalse()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Tags = new TagsCollection(
                new Tag("type", "restriction"),
                new Tag("restriction", "not_a_real_restriction"))
        };

        var result = parser.IsRestriction(relation, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_ViaNode_ProhibitoryRestriction_ShouldParseCorrectly()
    {
        var parser = new OsmTurnRestrictionParser();
        var osmGeos = new OsmGeo[]
        {
            new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
        };
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way),
                new RelationMember(2, "via", OsmGeoType.Node),
                new RelationMember(2, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "no_right_turn"))
        };

        var result = parser.TryParse(relation,
            k => osmGeos.First(x => x.Id == k && x.Type == OsmGeoType.Way) as Way,
            out var restriction);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        Assert.NotNull(restriction);
        Assert.True(restriction.IsProbibitory);
        Assert.Equal(1, restriction.From.First().Id);
        Assert.Equal(2, restriction.To.First().Id);
        Assert.Equal(2, restriction.ViaNodeId);
        Assert.Empty(restriction.Via);
    }

    [Fact]
    public void TryParse_ViaWay_ProhibitoryRestriction_ShouldParseCorrectly()
    {
        var parser = new OsmTurnRestrictionParser();
        var osmGeos = new OsmGeo[]
        {
            new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 3, Version = 1, Nodes = new[] { 3L, 4 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
        };
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way),
                new RelationMember(2, "via", OsmGeoType.Way),
                new RelationMember(3, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "no_right_turn"))
        };

        var result = parser.TryParse(relation,
            k => osmGeos.First(x => x.Id == k && x.Type == OsmGeoType.Way) as Way,
            out var restriction);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        Assert.NotNull(restriction);
        Assert.True(restriction.IsProbibitory);
        Assert.Equal(1, restriction.From.First().Id);
        Assert.Equal(2, restriction.Via.First().Id);
        Assert.Equal(3, restriction.To.First().Id);
        Assert.Null(restriction.ViaNodeId);
    }

    [Fact]
    public void TryParse_MandatoryRestriction_ShouldSetIsProhibitoryFalse()
    {
        var parser = new OsmTurnRestrictionParser();
        var osmGeos = new OsmGeo[]
        {
            new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
        };
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way),
                new RelationMember(2, "via", OsmGeoType.Node),
                new RelationMember(2, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "only_straight_on"))
        };

        var result = parser.TryParse(relation,
            k => osmGeos.First(x => x.Id == k && x.Type == OsmGeoType.Way) as Way,
            out var restriction);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        Assert.NotNull(restriction);
        Assert.False(restriction.IsProbibitory);
    }

    [Fact]
    public void TryParse_NonRestrictionRelation_ShouldReturnFalse()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Tags = new TagsCollection(new Tag("type", "multipolygon"))
        };

        var result = parser.TryParse(relation, _ => null, out var restriction);

        Assert.False(result.IsError);
        Assert.False(result.Value);
        Assert.Null(restriction);
    }

    [Fact]
    public void TryParse_MissingMemberWay_ShouldReturnError()
    {
        var parser = new OsmTurnRestrictionParser();
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way),
                new RelationMember(2, "via", OsmGeoType.Node),
                new RelationMember(2, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "no_right_turn"))
        };

        var result = parser.TryParse(relation, _ => null, out _);

        Assert.True(result.IsError);
    }

    [Fact]
    public void TryParse_NoViaPresent_ShouldReturnError()
    {
        var parser = new OsmTurnRestrictionParser();
        var osmGeos = new OsmGeo[]
        {
            new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
        };
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way),
                new RelationMember(2, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(new Tag("type", "restriction"), new Tag("restriction", "no_right_turn"))
        };

        var result = parser.TryParse(relation,
            k => osmGeos.First(x => x.Id == k && x.Type == OsmGeoType.Way) as Way,
            out _);

        Assert.True(result.IsError);
    }

    [Fact]
    public void TryParse_PreservesAttributes()
    {
        var parser = new OsmTurnRestrictionParser();
        var osmGeos = new OsmGeo[]
        {
            new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
            new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
        };
        var relation = new Relation
        {
            Id = 1, Version = 1,
            Members = new[]
            {
                new RelationMember(1, "from", OsmGeoType.Way),
                new RelationMember(2, "via", OsmGeoType.Node),
                new RelationMember(2, "to", OsmGeoType.Way)
            },
            Tags = new TagsCollection(
                new Tag("type", "restriction"),
                new Tag("restriction", "no_right_turn"))
        };

        var result = parser.TryParse(relation,
            k => osmGeos.First(x => x.Id == k && x.Type == OsmGeoType.Way) as Way,
            out var restriction);

        Assert.NotNull(restriction);
        var attrs = restriction.Attributes.ToList();
        Assert.Contains(("type", "restriction"), attrs);
        Assert.Contains(("restriction", "no_right_turn"), attrs);
    }
}
