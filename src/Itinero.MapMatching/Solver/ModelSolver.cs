using System;
using System.Collections.Generic;
using Itinero.MapMatching.Model;

namespace Itinero.MapMatching.Solver;

public class ModelSolver
{
    public IEnumerable<int> Solve(GraphModel model)
    {
        var binaryHeap = new BinaryHeap<uint>();
        if (model.FirstNode == null) throw new Exception("Model is empty");
        if (model.LastNode == null) throw new Exception("Model is not empty, should have a last node");

        var path = new List<int>();
        var pathTree = new PathTree();
        var pointer = pathTree.Add((uint)model.FirstNode.Value, uint.MaxValue);
        binaryHeap.Push(pointer, 0);

        var settled = new HashSet<int>();
        while (true)
        {
            pointer = binaryHeap.Pop(out var priority);
            pathTree.Get(pointer, out var node, out var previous);
            var popSuccess = !settled.Contains((int)node);
            while (binaryHeap.Count > 0 &&
                   !popSuccess)
            {
                pointer = binaryHeap.Pop(out priority);
                pathTree.Get(pointer, out node, out previous);
                popSuccess = !settled.Contains((int)node);
            }
            if (popSuccess == false) break;
            settled.Add((int)node);

            if (node == model.LastNode)
            {
                // TODO: compose final path.
                while (true)
                {
                    path.Add((int)node);
                    if (node == model.FirstNode)
                    {
                        path.Reverse();
                        break;
                    }

                    pointer = previous;
                    pathTree.Get(pointer, out node, out previous);
                }
                break;
            }

            // add neighbours.
            var graphNode = model.GetNode((int)node);
            var neighbours = model.GetNeighbours((int)node);
            foreach (var neighbour in neighbours)
            {
                var neighbourPointer = pathTree.Add((uint)neighbour.Node2, pointer);
                binaryHeap.Push(neighbourPointer, priority + graphNode.Cost + neighbour.Cost);
            }
        }

        return path;
    }
}
