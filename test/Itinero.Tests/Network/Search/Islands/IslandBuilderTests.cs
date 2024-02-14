using System.Linq;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

public class IslandBuilderTests
{
    [Fact]
    public void IslandBuilder_IsOnIsland_SingleEdge_ShouldReturnTrue()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
        }

        var islandBuilder = new IslandBuilder(routerDb.Latest, new IslandBuilderSettings()
        {
            Profile = new DefaultProfile()
        });
        var isOnIsland = islandBuilder.IsOnIsland(edge, true);

        Assert.True(isOnIsland);
    }

    [Fact]
    public void IslandBuilder_IsOnIsland_TwoEdges_MinSizeThree_ShouldReturnTrue()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
        }

        var islandBuilder = new IslandBuilder(routerDb.Latest, new IslandBuilderSettings()
        {
            Profile = new DefaultProfile(),
            MinIslandSize = 3
        });
        var isOnIsland = islandBuilder.IsOnIsland(edge, true);

        Assert.True(isOnIsland);
    }

    [Fact]
    public void IslandBuilder_IsOnIsland_TwoEdgesOneWay_MinSizeTwo_ShouldReturnTrue()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
        }

        var islandBuilder = new IslandBuilder(routerDb.Latest, new IslandBuilderSettings()
        {
            Profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(1, 0, 1, 0, true)),
            MinIslandSize = 2
        });
        var isOnIsland = islandBuilder.IsOnIsland(edge, true);

        Assert.True(isOnIsland);
    }

    [Fact]
    public void IslandBuilder_IsOnIsland_ThreeEdgeOneWayLoop_ShouldBeOneIslands()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
            writer.AddEdge(vertex3, vertex1);
        }

        var islandBuilder = new IslandBuilder(routerDb.Latest, new IslandBuilderSettings()
        {
            Profile = new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(1, 0, 1, 0, true)),
            MinIslandSize = 3
        });
        var isOnIsland = islandBuilder.IsOnIsland(edge, true);
        Assert.False(isOnIsland);

        Assert.Single(islandBuilder.GetLabels().Islands);
    }

    [Fact]
    public void IslandBuilder_IsOnIsland_OneEdgeWithThreeEdgeLoop_ShouldBeOneIsland()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
            writer.AddEdge(vertex3, vertex4);
            writer.AddEdge(vertex4, vertex2);
        }

        var islandBuilder = new IslandBuilder(routerDb.Latest, new IslandBuilderSettings()
        {
            Profile = new DefaultProfile(),
        });
        islandBuilder.IsOnIsland(edge, true);

        Assert.Single(islandBuilder.GetLabels().Islands);
    }

    [Fact]
    public void IslandBuilder_IsOnIsland_LoopWithOneWay_ShouldBeTwoIslands()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2, attributes: new[] { ("oneway", "yes") });
            writer.AddEdge(vertex2, vertex3);
            writer.AddEdge(vertex3, vertex4);
            writer.AddEdge(vertex4, vertex1);
        }

        var islandBuilder = new IslandBuilder(routerDb.Latest, new IslandBuilderSettings()
        {
            Profile = new DefaultProfile(getEdgeFactor: (a) =>
            {
                if (!a.Any()) return new EdgeFactor(1, 1, 1, 1);

                return new EdgeFactor(1, 0, 1, 0);
            }),
        });
        islandBuilder.IsOnIsland(edge, true);

        Assert.Equal(2, islandBuilder.GetLabels().Islands.Count());
    }

    [Fact]
    public void IslandBuilder_IsOnIsland_OneIslandWithNonConnectedEdge_ShouldBeTwoIslands()
    {
        var routerDb = new RouterDb();
        EdgeId islandEdge;
        EdgeId nonIslandEdge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex5 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            nonIslandEdge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
            writer.AddEdge(vertex3, vertex1);
            islandEdge = writer.AddEdge(vertex4, vertex5);
        }

        var islandBuilder = new IslandBuilder(routerDb.Latest, new IslandBuilderSettings()
        {
            Profile = new DefaultProfile(getEdgeFactor: (a) =>
            {
                if (!a.Any()) return new EdgeFactor(1, 1, 1, 1);

                return new EdgeFactor(1, 0, 1, 0);
            }),
            MinIslandSize = 3
        });
        Assert.True(islandBuilder.IsOnIsland(islandEdge, true));
        Assert.True(islandBuilder.IsOnIsland(islandEdge, false));
        Assert.False(islandBuilder.IsOnIsland(nonIslandEdge, true));
        Assert.False(islandBuilder.IsOnIsland(nonIslandEdge, false));
    }
}
