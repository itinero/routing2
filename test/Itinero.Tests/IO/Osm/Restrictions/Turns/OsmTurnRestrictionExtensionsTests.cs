using System;
using System.Linq;
using Itinero.IO.Osm.Restrictions.Turns;
using Itinero.Network.Tiles.Standalone.Global;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions.Turns;

public class OsmTurnRestrictionExtensionsTests
{
    [Fact]
    public void ToGlobalNetworkRestrictions_ViaNode_ShouldReturnSingleRestrictionWith2Edges()
    {
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            2,
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) } });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Single(results);
        var globalRestriction = results[0];
        Assert.True(globalRestriction.IsProhibitory);
        Assert.Equal(2, globalRestriction.Count);

        // from way: nodes [1,2], via=2 → last node matches via → GlobalEdgeId(1, 0, 1)
        Assert.Equal(GlobalEdgeId.Create(1, 0, 1), globalRestriction[0]);
        // to way: nodes [2,3], via=2 → first node matches via → GlobalEdgeId(2, 0, 1)
        Assert.Equal(GlobalEdgeId.Create(2, 0, 1), globalRestriction[1]);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_ViaNode_MandatoryRestriction_ShouldNotBeProhibitory()
    {
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            2,
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            isProhibitory: false);

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Single(results);
        Assert.False(results[0].IsProhibitory);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_ViaNode_FromWayReversed_ShouldReturnInvertedFromEdge()
    {
        // From way has nodes [2,1] — via node 2 is the FIRST node, so the edge is inverted.
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 2L, 1 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            2,
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) } });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Single(results);
        // from way: nodes [2,1], via=2 → first node matches via → GlobalEdgeId(1, 1, 0) (reversed)
        Assert.Equal(GlobalEdgeId.Create(1, 1, 0), results[0][0]);
        // to way: unchanged
        Assert.Equal(GlobalEdgeId.Create(2, 0, 1), results[0][1]);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_ViaNode_ToWayReversed_ShouldReturnInvertedToEdge()
    {
        // To way has nodes [3,2] — via node 2 is the LAST node, so the edge is inverted.
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            2,
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 3L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Single(results);
        // from: unchanged
        Assert.Equal(GlobalEdgeId.Create(1, 0, 1), results[0][0]);
        // to way: nodes [3,2], via=2 → last node matches via → GlobalEdgeId(2, 1, 0) (reversed)
        Assert.Equal(GlobalEdgeId.Create(2, 1, 0), results[0][1]);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_ViaWay_ShouldReturnSingleRestrictionWith3Edges()
    {
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 10L, 1, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            new[] { new Way { Id = 3, Version = 1, Nodes = new[] { 3L, 4, 11 }, Tags = new TagsCollection(new Tag("highway", "residential")) } });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Single(results);
        var globalRestriction = results[0];
        Assert.Equal(3, globalRestriction.Count);

        // from way: nodes [10,1,2], via-tail=2 → last node matches → GlobalEdgeId(1, 0, 2)
        Assert.Equal(GlobalEdgeId.Create(1, 0, 2), globalRestriction[0]);
        // via way: nodes [2,3], starts at node 2 → GlobalEdgeId(2, 0, 1)
        Assert.Equal(GlobalEdgeId.Create(2, 0, 1), globalRestriction[1]);
        // to way: nodes [3,4,11], via-head=3 → first node matches → GlobalEdgeId(3, 0, 2)
        Assert.Equal(GlobalEdgeId.Create(3, 0, 2), globalRestriction[2]);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_ViaWayReversed_ShouldFlipViaEdge()
    {
        // Via way is [3,2] but connects from→via at node 2 and via→to at node 3.
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 10L, 1, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 3L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            new[] { new Way { Id = 3, Version = 1, Nodes = new[] { 3L, 4, 11 }, Tags = new TagsCollection(new Tag("highway", "residential")) } });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Single(results);
        var globalRestriction = results[0];
        Assert.Equal(3, globalRestriction.Count);

        // from: same as before
        Assert.Equal(GlobalEdgeId.Create(1, 0, 2), globalRestriction[0]);
        // via way: nodes [3,2], enters at node 2 (last) → GlobalEdgeId(2, 1, 0) (reversed)
        Assert.Equal(GlobalEdgeId.Create(2, 1, 0), globalRestriction[1]);
        // to: same as before
        Assert.Equal(GlobalEdgeId.Create(3, 0, 2), globalRestriction[2]);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_MultipleFromWays_ShouldReturnMultipleRestrictions()
    {
        // Two from ways both connecting to via node 2.
        var restriction = OsmTurnRestriction.Create(
            new[]
            {
                new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
                new Way { Id = 10, Version = 1, Nodes = new[] { 10L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) }
            },
            2,
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) } });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Equal(2, results.Count);

        // first from way: id=1, nodes [1,2]
        Assert.Equal(GlobalEdgeId.Create(1, 0, 1), results[0][0]);
        Assert.Equal(GlobalEdgeId.Create(2, 0, 1), results[0][1]);

        // second from way: id=10, nodes [10,2]
        Assert.Equal(GlobalEdgeId.Create(10, 0, 1), results[1][0]);
        Assert.Equal(GlobalEdgeId.Create(2, 0, 1), results[1][1]);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_MultipleToWays_ShouldReturnMultipleRestrictions()
    {
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 1L, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            2,
            new[]
            {
                new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) },
                new Way { Id = 20, Version = 1, Nodes = new[] { 2L, 20 }, Tags = new TagsCollection(new Tag("highway", "residential")) }
            });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Equal(2, results.Count);

        // first to way
        Assert.Equal(GlobalEdgeId.Create(1, 0, 1), results[0][0]);
        Assert.Equal(GlobalEdgeId.Create(2, 0, 1), results[0][1]);

        // second to way
        Assert.Equal(GlobalEdgeId.Create(1, 0, 1), results[1][0]);
        Assert.Equal(GlobalEdgeId.Create(20, 0, 1), results[1][1]);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_FromWayWithMultipleNodes_ShouldUseFullWaySpan()
    {
        // From way has 4 nodes — the GlobalEdgeId should span from index 0 to index 3.
        var restriction = OsmTurnRestriction.Create(
            new[] { new Way { Id = 1, Version = 1, Nodes = new[] { 10L, 11, 12, 2 }, Tags = new TagsCollection(new Tag("highway", "residential")) } },
            2,
            new[] { new Way { Id = 2, Version = 1, Nodes = new[] { 2L, 3 }, Tags = new TagsCollection(new Tag("highway", "residential")) } });

        var results = restriction.ToGlobalNetworkRestrictions().ToList();

        Assert.Single(results);
        // from way: nodes [10,11,12,2], via=2 → last node matches → GlobalEdgeId(1, 0, 3)
        Assert.Equal(GlobalEdgeId.Create(1, 0, 3), results[0][0]);
    }
}
