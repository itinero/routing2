using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;
using Itinero.Network.Search.Islands;
using Itinero.Profiles;
using Xunit;

namespace Itinero.Tests.Network.Search.Islands;

public class IslandBuilderTests
{
    [Fact]
    public async Task IslandBuilder_IsOnIsland_SingleEdge_ShouldReturnTrue()
    {
        var routerDb = new RouterDb();
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
        }

        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize),
            routerDb.Latest.GetCostFunctionFor(new DefaultProfile()), edge);

        Assert.True(isOnIsland);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_TwoEdges_MaxSizeTwo_ShouldReturnFalse()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            MaxIslandSize = 2
        });
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
        }
        
        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize),
            routerDb.Latest.GetCostFunctionFor(new DefaultProfile()), edge);

        Assert.False(isOnIsland);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_TwoEdges_MaxSizeThree_ShouldReturnTrue()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            MaxIslandSize = 3
        });
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
        }
        
        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize),
            routerDb.Latest.GetCostFunctionFor(new DefaultProfile()), edge);

        Assert.True(isOnIsland);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_TwoEdgesOneWay_MinSizeTwo_ShouldReturnTrue()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            MaxIslandSize = 2
        });
        EdgeId edge;
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edge = writer.AddEdge(vertex1, vertex2);
            writer.AddEdge(vertex2, vertex3);
        }
        
        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize),
            routerDb.Latest.GetCostFunctionFor(new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(1, 0, 1, 0, true))), edge);

        Assert.True(isOnIsland);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_ThreeEdgeOneWayLoop_ShouldBeOneIslands()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            MaxIslandSize = 3
        });
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
        
        var labels = new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize);
        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, 
            routerDb.Latest.GetCostFunctionFor(new DefaultProfile(getEdgeFactor: (_) => new EdgeFactor(1, 0, 1, 0, true))), edge);

        Assert.False(isOnIsland);

        Assert.Single(labels.Islands);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_OneEdgeWithThreeEdgeLoop_ShouldBeOneIsland()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            MaxIslandSize = 4
        });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edges.Add(writer.AddEdge(vertex1, vertex2));
            edges.Add(writer.AddEdge(vertex2, vertex3));
            edges.Add(writer.AddEdge(vertex3, vertex4));
            edges.Add(writer.AddEdge(vertex4, vertex2));
        }

        var labels = new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize);
        foreach (var edge in edges)
        {
            await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels,
                routerDb.Latest.GetCostFunctionFor(new DefaultProfile()), edge);
            await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels,
                routerDb.Latest.GetCostFunctionFor(new DefaultProfile()), edge);
        }

        Assert.Single(labels.Islands);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_LoopWithOneWay_ShouldBeOneIsland()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            MaxIslandSize = 3
        });
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edges.Add(writer.AddEdge(vertex1, vertex2, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(vertex2, vertex3));
            edges.Add(writer.AddEdge(vertex3, vertex1));
        }

        var labels = new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize);
        var profile = new DefaultProfile(getEdgeFactor: (a) =>
        {
            if (!a.Any()) return new EdgeFactor(1, 1, 1, 1);

            return new EdgeFactor(1, 0, 1, 0);
        });
        
        Assert.False(await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels,
            routerDb.Latest.GetCostFunctionFor(profile), edges[0]));
        Assert.False(await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels,
            routerDb.Latest.GetCostFunctionFor(profile), edges[1]));
        Assert.False(await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels,
            routerDb.Latest.GetCostFunctionFor(profile), edges[2]));

        Assert.Single(labels.Islands);
    }

    [Fact]
    public async Task IslandBuilder_IsOnIsland_OneIslandWithNonConnectedEdge_ShouldBeTwoIslands()
    {
        var routerDb = new RouterDb(new RouterDbConfiguration()
        {
            MaxIslandSize = 3
        });
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

        var profile = new DefaultProfile(getEdgeFactor: (a) =>
        {
            if (!a.Any()) return new EdgeFactor(1, 1, 1, 1);

            return new EdgeFactor(1, 0, 1, 0);
        });

        var labels = new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize);
        Assert.True(await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels,
            routerDb.Latest.GetCostFunctionFor(profile), islandEdge));
        Assert.False(await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels,
            routerDb.Latest.GetCostFunctionFor(profile), nonIslandEdge));
    }
    
    [Fact]
    public async Task IslandBuilder_IsOnIsland_EdgeConnectedToNoneIslandNeighbour_ShouldReturnFalse()
    {
        var routerDb = new RouterDb();
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edges.Add(writer.AddEdge(vertex1, vertex2));
            edges.Add(writer.AddEdge(vertex2, vertex3));
        }
        
        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize),
            routerDb.Latest.GetCostFunctionFor(new DefaultProfile()), edges[0], e =>
            {
                if (e.EdgeId == edges[1]) return false;

                return null;
            });

        Assert.False(isOnIsland);
    }
    
    [Fact]
    public async Task IslandBuilder_IsOnIsland_EdgeConnectedOneWithOneWay_ShouldReturnTrue()
    {
        var routerDb = new RouterDb();
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edges.Add(writer.AddEdge(vertex1, vertex2));
            edges.Add(writer.AddEdge(vertex2, vertex3, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(vertex3, vertex4));
        }

        var labels = new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize);
        var profile = new DefaultProfile(getEdgeFactor: (a) =>
        {
            if (!a.Any()) return new EdgeFactor(1, 1, 1, 1);

            return new EdgeFactor(1, 0, 1, 0);
        });
        var costFunction = routerDb.Latest.GetCostFunctionFor(profile);
        Func<IEdgeEnumerator, bool?> isOnIslandAlready = e =>
        {
            if (e.EdgeId == edges[0]) return false;

            return null;
        };
        
        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, costFunction,
            edges[0], isOnIslandAlready);
        isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, costFunction,
            edges[1], isOnIslandAlready);
        isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, costFunction,
            edges[2], isOnIslandAlready);

        Assert.True(isOnIsland);
    }
    
    [Fact]
    public async Task IslandBuilder_IsOnIsland_EdgeConnectedOneWithTwoOneWays_ShouldReturnTrue()
    {
        var routerDb = new RouterDb();
        var edges = new List<EdgeId>();
        using (var writer = routerDb.GetMutableNetwork())
        {
            var vertex1 = writer.AddVertex(4.792613983154297, 51.26535213392538);
            var vertex2 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex3 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            var vertex4 = writer.AddVertex(4.797506332397461, 51.26674845584085);
            edges.Add(writer.AddEdge(vertex1, vertex2));
            edges.Add(writer.AddEdge(vertex2, vertex3, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(vertex1, vertex4, attributes: new[] { ("oneway", "yes") }));
            edges.Add(writer.AddEdge(vertex3, vertex4));
        }

        var labels = new IslandLabels(routerDb.Latest.IslandManager.MaxIslandSize);
        var profile = new DefaultProfile(getEdgeFactor: (a) =>
        {
            if (!a.Any()) return new EdgeFactor(1, 1, 1, 1);

            return new EdgeFactor(1, 0, 1, 0);
        });
        var costFunction = routerDb.Latest.GetCostFunctionFor(profile);
        Func<IEdgeEnumerator, bool?> isOnIslandAlready = e =>
        {
            if (e.EdgeId == edges[0]) return false;

            return null;
        };
        
        var isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, costFunction,
            edges[0], isOnIslandAlready);
        isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, costFunction,
            edges[1], isOnIslandAlready);
        isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, costFunction,
            edges[2], isOnIslandAlready);
        isOnIsland = await IslandBuilder.IsOnIslandAsync(routerDb.Latest, labels, costFunction,
            edges[3], isOnIslandAlready);

        Assert.True(isOnIsland);
    }
}
