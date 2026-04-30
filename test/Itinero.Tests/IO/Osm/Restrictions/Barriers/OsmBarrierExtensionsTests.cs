using System.Collections.Generic;
using System.Linq;
using Itinero.IO.Osm.Restrictions.Barriers;
using Itinero.Network.Tiles.Standalone.Global;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Restrictions.Barriers;

public class OsmBarrierExtensionsTests
{
    [Fact]
    public void ToGlobalNetworkRestrictions_SingleWayBollardAtEnd_ShouldReturnNoRestrictions()
    {
        // Way [a, bollard]. Only one tail-hop (way, 0, 1). Cross-product needs two distinct hops.
        var bollard = new Node { Id = 2, Tags = new TagsCollection(new Tag("barrier", "bollard")) };
        var ways = new[]
        {
            new Way { Id = 1, Nodes = new long[] { 1, 2 } }
        };

        var results = OsmBarrier.Create(bollard, ways).ToGlobalNetworkRestrictions().ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_SingleWayBollardInMiddle_ShouldReturnTwoThroughRestrictions()
    {
        // Way [a, bollard, b]. Bollard at index 1.
        // tail-hops: (1,0,1) forward, (1,2,1) reverse.
        var bollard = new Node { Id = 2, Tags = new TagsCollection(new Tag("barrier", "bollard")) };
        var ways = new[]
        {
            new Way { Id = 1, Nodes = new long[] { 1, 2, 3 } }
        };

        var results = OsmBarrier.Create(bollard, ways).ToGlobalNetworkRestrictions().ToList();

        Assert.Equal(2, results.Count);

        // restriction A: enter via a→bollard, exit bollard→b.
        Assert.Equal(2, results[0].Count);
        Assert.Equal(GlobalEdgeId.Create(1, 0, 1), results[0][0]);
        Assert.Equal(GlobalEdgeId.Create(1, 1, 2), results[0][1]);

        // restriction B: enter via b→bollard, exit bollard→a.
        Assert.Equal(2, results[1].Count);
        Assert.Equal(GlobalEdgeId.Create(1, 2, 1), results[1][0]);
        Assert.Equal(GlobalEdgeId.Create(1, 1, 0), results[1][1]);

        // chain-connectivity: previous.Head and current.Tail must point at the same node-index in the same way.
        AssertChainConnectivity(results, ways);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_TwoWaysMeetAtBollard_ShouldReturnFourThroughRestrictions()
    {
        // Way 1: [a, bollard]. Way 2: [bollard, b].
        // tail-hops: (1, 0, 1) for way1 forward, (2, 1, 0) for way2 reverse.
        var bollard = new Node { Id = 2, Tags = new TagsCollection(new Tag("barrier", "bollard")) };
        var ways = new[]
        {
            new Way { Id = 1, Nodes = new long[] { 1, 2 } },
            new Way { Id = 2, Nodes = new long[] { 2, 3 } }
        };

        var results = OsmBarrier.Create(bollard, ways).ToGlobalNetworkRestrictions().ToList();

        Assert.Equal(2, results.Count);

        // restriction A: way1 a→bollard, way2 bollard→b.
        Assert.Equal(GlobalEdgeId.Create(1, 0, 1), results[0][0]);
        Assert.Equal(GlobalEdgeId.Create(2, 0, 1), results[0][1]);

        // restriction B: way2 b→bollard, way1 bollard→a.
        Assert.Equal(GlobalEdgeId.Create(2, 1, 0), results[1][0]);
        Assert.Equal(GlobalEdgeId.Create(1, 1, 0), results[1][1]);

        AssertChainConnectivity(results, ways);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_BollardAtSharedNodeOfMultiNodeWays_ShouldSpanFullWays()
    {
        // Way 1: [a, x, bollard]. Way 2: [bollard, y, b].
        // tail-hops: (1, 0, 2) for way1 forward, (2, 2, 0) for way2 reverse.
        // way-spanning encoding — Head of tail-hop and Tail of head-hop both point to the bollard.
        var bollard = new Node { Id = 99, Tags = new TagsCollection(new Tag("barrier", "bollard")) };
        var ways = new[]
        {
            new Way { Id = 1, Nodes = new long[] { 1, 10, 99 } },
            new Way { Id = 2, Nodes = new long[] { 99, 20, 3 } }
        };

        var results = OsmBarrier.Create(bollard, ways).ToGlobalNetworkRestrictions().ToList();

        Assert.Equal(2, results.Count);

        Assert.Equal(GlobalEdgeId.Create(1, 0, 2), results[0][0]);
        Assert.Equal(GlobalEdgeId.Create(2, 0, 2), results[0][1]);

        Assert.Equal(GlobalEdgeId.Create(2, 2, 0), results[1][0]);
        Assert.Equal(GlobalEdgeId.Create(1, 2, 0), results[1][1]);

        AssertChainConnectivity(results, ways);
    }

    [Fact]
    public void ToGlobalNetworkRestrictions_ThreeWaysMeetAtBollard_ShouldReturnSixRestrictions()
    {
        // Three ways all ending at the bollard — N*(N-1) = 6 ordered pairs.
        var bollard = new Node { Id = 0, Tags = new TagsCollection(new Tag("barrier", "bollard")) };
        var ways = new[]
        {
            new Way { Id = 1, Nodes = new long[] { 1, 0 } },
            new Way { Id = 2, Nodes = new long[] { 2, 0 } },
            new Way { Id = 3, Nodes = new long[] { 3, 0 } }
        };

        var results = OsmBarrier.Create(bollard, ways).ToGlobalNetworkRestrictions().ToList();

        Assert.Equal(6, results.Count);
        AssertChainConnectivity(results, ways);
    }

    private static void AssertChainConnectivity(IReadOnlyList<GlobalRestriction> restrictions, IEnumerable<Way> ways)
    {
        var wayById = ways.ToDictionary(w => w.Id!.Value);

        foreach (var restriction in restrictions)
        {
            for (var i = 1; i < restriction.Count; i++)
            {
                var previous = restriction[i - 1];
                var current = restriction[i];

                var previousNode = wayById[previous.EdgeId].Nodes[previous.Head];
                var currentNode = wayById[current.EdgeId].Nodes[current.Tail];

                Assert.Equal(previousNode, currentNode);
            }
        }
    }
}
