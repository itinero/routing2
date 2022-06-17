using System.Linq;
using Itinero.IO.Osm.Restrictions;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions;

public class OsmTurnRestrictionExtensionsTests
{
    [Fact]
    public void OsmTurnRestrictionExtensions_GetViaFrom_RestrictionWithViaNode_ShouldReturnViaNode()
    {
        var osmTurnRestriction = OsmTurnRestriction.Create(new[]
            {
                new Way()
                {
                    Id = 1,
                    Version = 1,
                    Nodes = new[] { 1L, 2 },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                }
            },
            2, new[]
            {
                new Way()
                {
                    Id = 2,
                    Version = 1,
                    Nodes = new[] { 2L, 3 },
                    Tags = new TagsCollection(new Tag("highway", "residential"))
                }
            });

        var viaResult = osmTurnRestriction.GetViaFrom();
        Assert.False(viaResult.IsError);
        var via = viaResult.Value;
        Assert.Equal(2, via);
    }

    [Fact]
    public void OsmTurnRestrictionExtensions_GetFromHop_RestrictionWithFromWayForward_ShouldReturnLastTwoNodeHop()
    {
        var osmTurnRestriction = OsmTurnRestriction.Create(new[]
        {
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 4, 1L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, 2, new[]
        {
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        });

        var fromHops = osmTurnRestriction.GetFromHops();
        Assert.NotNull(fromHops);
        Assert.Single(fromHops);
        var fromHopResult = fromHops.First();
        Assert.False(fromHopResult.IsError);
        var fromHop = fromHopResult.Value;
        Assert.Equal(1, fromHop.way.Id);
        Assert.Equal(0, fromHop.minStartNode);
        Assert.Equal(2, fromHop.endNode);
    }

    [Fact]
    public void OsmTurnRestrictionExtensions_GetFromHop_RestrictionWithFromWayBackward_ShouldReturnFirstTwoNodeHop()
    {
        var osmTurnRestriction = OsmTurnRestriction.Create(new[]
        {
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 2, 1L, 4 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, 2, new[]
        {
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 2L, 3 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        });

        var fromHops = osmTurnRestriction.GetFromHops();
        Assert.NotNull(fromHops);
        Assert.Single(fromHops);
        var fromHopResult = fromHops.First();
        Assert.False(fromHopResult.IsError);
        var fromHop = fromHopResult.Value;
        Assert.Equal(1, fromHop.way.Id);
        Assert.Equal(2, fromHop.minStartNode);
        Assert.Equal(0, fromHop.endNode);
    }

    [Fact]
    public void OsmTurnRestrictionExtensions_GetViaFrom_RestrictionWithViaWay_ShouldReturnNodeInCommon()
    {
        var osmTurnRestriction = OsmTurnRestriction.Create(new[]
        {
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 10L, 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 2, 3L },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 3,
                Version = 1,
                Nodes = new[] { 3L, 4, 11L },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        });

        var viaResult = osmTurnRestriction.GetViaFrom();
        Assert.False(viaResult.IsError);
        var via = viaResult.Value;
        Assert.Equal(2, via);
    }

    [Fact]
    public void OsmTurnRestrictionExtensions_GetViaTo_RestrictionWithViaWay_ShouldReturnNodeInCommon()
    {
        var osmTurnRestriction = OsmTurnRestriction.Create(new[]
        {
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 10L, 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 2, 3L },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 3,
                Version = 1,
                Nodes = new[] { 3L, 4, 11L },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        });

        var viaResult = osmTurnRestriction.GetViaTo();
        Assert.False(viaResult.IsError);
        var via = viaResult.Value;
        Assert.Equal(3, via);
    }

    [Fact]
    public void OsmTurnRestrictionExtensions_GetViaFrom_RestrictionWithViaWayReversed_ShouldReturnNodeInCommon()
    {
        var osmTurnRestriction = OsmTurnRestriction.Create(new[]
        {
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 10L, 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 3L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 3,
                Version = 1,
                Nodes = new[] { 3L, 4, 11L },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        });

        var viaResult = osmTurnRestriction.GetViaFrom();
        Assert.False(viaResult.IsError);
        var via = viaResult.Value;
        Assert.Equal(2, via);
    }

    [Fact]
    public void OsmTurnRestrictionExtensions_GetViaTo_RestrictionWithViaWayReversed_ShouldReturnNodeInCommon()
    {
        var osmTurnRestriction = OsmTurnRestriction.Create(new[]
        {
            new Way()
            {
                Id = 1,
                Version = 1,
                Nodes = new[] { 10L, 1, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 2,
                Version = 1,
                Nodes = new[] { 3L, 2 },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        }, new[]
        {
            new Way()
            {
                Id = 3,
                Version = 1,
                Nodes = new[] { 3L, 4, 11L },
                Tags = new TagsCollection(new Tag("highway", "residential"))
            }
        });

        var viaResult = osmTurnRestriction.GetViaTo();
        Assert.False(viaResult.IsError);
        var via = viaResult.Value;
        Assert.Equal(3, via);
    }
}