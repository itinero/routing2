using System;
using System.Linq;
using Itinero.MapMatching.Model;
using Itinero.MapMatching.Solver;
using Xunit;

namespace Itinero.MapMatching.Tests;

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
}
