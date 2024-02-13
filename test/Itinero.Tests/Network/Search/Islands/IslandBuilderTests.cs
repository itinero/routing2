using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

public class IslandBuilderTests
{
    [Fact]
    public async Task IslandBuilder_IsOnIsland_SingleEdge_ShouldReturnTrueAsync()
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
        var isOnIsland = await islandBuilder.IsOnIslandAsync(edge, true);

        Assert.True(isOnIsland);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_TwoEdges_MinSizeThree_ShouldReturnTrueAsync()
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
        var isOnIsland = await islandBuilder.IsOnIslandAsync(edge, true);

        Assert.True(isOnIsland);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_TwoEdgesOneWay_MinSizeTwo_ShouldReturnTrueAsync()
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
        var isOnIsland = await islandBuilder.IsOnIslandAsync(edge, true);

        Assert.True(isOnIsland);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_ThreeEdgeOneWayLoop_ShouldBeOneIslandsAsync()
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
        var isOnIsland = await islandBuilder.IsOnIslandAsync(edge, true);
        Assert.False(isOnIsland);

        Assert.Single(islandBuilder.GetLabels().Islands);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_OneEdgeWithThreeEdgeLoop_ShouldBeOneIslandAsync()
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
        await islandBuilder.IsOnIslandAsync(edge, true);

        Assert.Single(islandBuilder.GetLabels().Islands);
    }
}
