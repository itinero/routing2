using System;
using System.Linq;
using Itinero.MapMatching;
using Itinero.MapMatching.Model;
using Itinero.MapMatching.Solver;
using Xunit;

namespace Itinero.Tests.MapMatching;

public class ModelSolverTests
{
    [Fact]
    public void ModelSolver_OneEdgeModel()
    {
        var model = new GraphModel(new Track(ArraySegment<TrackPoint>.Empty));

        var node0 = model.AddNode(new GraphNode());
        var node1 = model.AddNode(new GraphNode()
        {
            Cost = 1
        });
        model.AddEdge(new GraphEdge()
        {
            Node1 = node0,
            Node2 = node1,
            Cost = 10
        });
        var node2 = model.AddNode(new GraphNode()
        {
            Cost = 1
        });
        model.AddEdge(new GraphEdge()
        {
            Node1 = node1,
            Node2 = node2,
            Cost = 10
        });
        var node3 = model.AddNode(new GraphNode());
        model.AddEdge(new GraphEdge()
        {
            Node1 = node2,
            Node2 = node3
        });

        var solver = new ModelSolver();
        var path = solver.Solve(model).ToList();
        Assert.Equal(4, path.Count);
        Assert.Equal(0, path[0]);
        Assert.Equal(1, path[1]);
        Assert.Equal(2, path[2]);
        Assert.Equal(3, path[3]);
    }

    [Fact]
    public void ModelSolver_PicksCheapestOfTwoParallelPaths()
    {
        // Two candidates at each layer, solver should pick the cheapest overall path.
        //
        // Layer 1: A(cost=5), B(cost=2)
        // Layer 2: C(cost=3), D(cost=8)
        // All transition costs = 1
        //
        // Expected: start -> B -> C -> end (total: 0+2+1+3+0 = 6)

        var model = new GraphModel(new Track(ArraySegment<TrackPoint>.Empty));

        var startNode = model.AddNode(new GraphNode()); // 0
        var nodeA = model.AddNode(new GraphNode() { Cost = 5 }); // 1
        var nodeB = model.AddNode(new GraphNode() { Cost = 2 }); // 2
        var nodeC = model.AddNode(new GraphNode() { Cost = 3 }); // 3
        var nodeD = model.AddNode(new GraphNode() { Cost = 8 }); // 4
        var endNode = model.AddNode(new GraphNode()); // 5

        // Start -> Layer 1
        model.AddEdge(new GraphEdge() { Node1 = startNode, Node2 = nodeA, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = startNode, Node2 = nodeB, Cost = 0 });

        // Layer 1 -> Layer 2
        model.AddEdge(new GraphEdge() { Node1 = nodeA, Node2 = nodeC, Cost = 1 });
        model.AddEdge(new GraphEdge() { Node1 = nodeA, Node2 = nodeD, Cost = 1 });
        model.AddEdge(new GraphEdge() { Node1 = nodeB, Node2 = nodeC, Cost = 1 });
        model.AddEdge(new GraphEdge() { Node1 = nodeB, Node2 = nodeD, Cost = 1 });

        // Layer 2 -> End
        model.AddEdge(new GraphEdge() { Node1 = nodeC, Node2 = endNode, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = nodeD, Node2 = endNode, Cost = 0 });

        var solver = new ModelSolver();
        var path = solver.Solve(model).ToList();

        Assert.Equal(4, path.Count);
        Assert.Equal(startNode, path[0]);
        Assert.Equal(nodeB, path[1]); // cheapest in layer 1
        Assert.Equal(nodeC, path[2]); // cheapest in layer 2
        Assert.Equal(endNode, path[3]);
    }

    [Fact]
    public void ModelSolver_FindsGlobalOptimumNotGreedy()
    {
        // The greedy choice at layer 1 (A, cost=1) leads to a worse global result
        // than the non-greedy choice (B, cost=5).
        //
        // Layer 1: A(cost=1), B(cost=5)
        // Layer 2: C(cost=10), D(cost=1)
        // Edges: A->C(0), A->D(100), B->C(0), B->D(0)
        //
        // Greedy picks A, then best from A: A->C = 1+0+10 = 11
        // Global optimum: B->D = 5+0+1 = 6

        var model = new GraphModel(new Track(ArraySegment<TrackPoint>.Empty));

        var startNode = model.AddNode(new GraphNode()); // 0
        var nodeA = model.AddNode(new GraphNode() { Cost = 1 }); // 1
        var nodeB = model.AddNode(new GraphNode() { Cost = 5 }); // 2
        var nodeC = model.AddNode(new GraphNode() { Cost = 10 }); // 3
        var nodeD = model.AddNode(new GraphNode() { Cost = 1 }); // 4
        var endNode = model.AddNode(new GraphNode()); // 5

        model.AddEdge(new GraphEdge() { Node1 = startNode, Node2 = nodeA, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = startNode, Node2 = nodeB, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = nodeA, Node2 = nodeC, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = nodeA, Node2 = nodeD, Cost = 100 }); // expensive
        model.AddEdge(new GraphEdge() { Node1 = nodeB, Node2 = nodeC, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = nodeB, Node2 = nodeD, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = nodeC, Node2 = endNode, Cost = 0 });
        model.AddEdge(new GraphEdge() { Node1 = nodeD, Node2 = endNode, Cost = 0 });

        var solver = new ModelSolver();
        var path = solver.Solve(model).ToList();

        Assert.Equal(4, path.Count);
        Assert.Equal(startNode, path[0]);
        Assert.Equal(nodeB, path[1]); // NOT nodeA (greedy would pick A)
        Assert.Equal(nodeD, path[2]); // NOT nodeC
        Assert.Equal(endNode, path[3]);
    }
}
