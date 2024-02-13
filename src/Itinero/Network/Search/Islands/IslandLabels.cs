using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Itinero.Routing.DataStructures;

namespace Itinero.Network.Search.Islands;

/// <summary>
/// Keeps an island label for edges for a fixed profile.
/// </summary>
public class IslandLabels
{
    /// <summary>
    /// Keeps a label for each edge.
    /// - Starting each edge starts with it's own unique Id.
    /// - When edges are bidirectionally connected they get take on the lowest Id of their neighbour.
    /// </summary>
    private readonly Dictionary<(EdgeId id, bool forward), uint> _labels = new();
    private readonly Dictionary<uint, (uint size, bool canGrow)> _islands = new(); // holds the size per island.
    private readonly IslandLabelGraph _labelGraph = new();

    /// <summary>
    /// The size an island has when it's not considered as an island anymore.
    /// </summary>
    public const uint NotAnIslandSize = uint.MaxValue;

    /// <summary>
    /// The label edges get when they are not considered any island anymore.
    /// </summary>
    public const uint NotAnIslandLabel = uint.MaxValue;

    /// <summary>
    /// Gets all the islands.
    /// </summary>
    public IEnumerable<(uint label, uint size, bool canGrow)> Islands
    {
        get
        {
            foreach (var (label, (size, canGrow)) in _islands)
            {
                yield return (label, size, canGrow);
            }
        }
    }

    /// <summary>
    /// Creates a new label.
    /// </summary>
    /// <returns></returns>
    public (uint label, uint size, bool canGrow) AddNew((EdgeId id, bool forward) edge)
    {
        if (_labels.ContainsKey(edge)) throw new ArgumentOutOfRangeException(nameof(edge));

        var label = _labelGraph.AddVertex();

        _labels[edge] = label;
        _islands[label] = (1, true);

        return (label, 1, true);
    }

    /// <summary>
    /// Adds an edge to an already existing island.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="edge"></param>
    /// <returns></returns>
    public void AddTo(uint label, (EdgeId id, bool forward) edge)
    {
        if (!_islands.TryGetValue(label, out var islandDetails)) throw new ArgumentOutOfRangeException(nameof(label));

        _islands[label] = (islandDetails.size + 1, true);
        _labels[edge] = label;
    }

    /// <summary>
    /// Connects the islands representing the two labels, tail -> head.
    /// </summary>
    /// <param name="tail">The tail label.</param>
    /// <param name="head">The head label.</param>
    internal bool ConnectTo(uint tail, uint head)
    {
        if (tail == head) return false;

        if (_labelGraph.HasEdge(tail, head)) return false;

        var headHasEdge = _labelGraph.HasEdge(head);
        _labelGraph.AddEdge(tail, head);

        if (headHasEdge) return this.FindLoops();
        return false;
    }

    /// <summary>
    /// Gets the label for the given edge, if any.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="label"></param>
    /// <returns></returns>
    public bool TryGet((EdgeId id, bool forward) edge, out uint label)
    {
        return _labels.TryGetValue(edge, out label);
    }

    /// <summary>
    /// Gets the island label for the given edge and its size, if any.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="island"></param>
    /// <returns></returns>
    public bool TryGetWithDetails((EdgeId id, bool forward) edge, out (uint label, uint size, bool canGrow) island)
    {
        island = (0, 0, true);
        if (!_labels.TryGetValue(edge, out var label)) return false;

        if (!_islands.TryGetValue(label, out var islandState))
            throw new Exception("Island does not exist");

        island = (label, islandState.size, islandState.canGrow);
        return true;
    }

    /// <summary>
    /// Sets the given island as complete, it cannot grow any further.
    /// </summary>
    /// <param name="label">The label of the island.</param>
    public void SetAsComplete(uint label)
    {
        if (!_islands.TryGetValue(label, out var islandState))
            throw new Exception("Island does not exist");

        _islands[label] = (islandState.size, false);
    }

    /// <summary>
    /// Finds loops in the island graph and connects looped islands.
    /// </summary>
    /// <returns>True if a connection was made, false otherwise.</returns>
    public bool FindLoops()
    {
        // TODO: it's probably better to call reduce here when too much has changed.

        var hasMadeConnection = false;
        var pathTree = new PathTree();
        var enumerator = _labelGraph.GetEdgeEnumerator();
        var settled = new HashSet<uint>();
        var queue = new Queue<uint>();
        var loop = new HashSet<uint>(); // keeps all with a path back to label, initially only label.
        uint label = 0;
        while (label < _labelGraph.VertexCount)
        {
            if (!enumerator.MoveTo(label))
            {
                label++;
                continue;
            }

            queue.Clear();
            pathTree.Clear();
            settled.Clear();

            loop.Add(label);
            queue.Enqueue(pathTree.Add(label, uint.MaxValue));

            while (queue.Count > 0)
            {
                var pointer = queue.Dequeue();
                pathTree.Get(pointer, out var current, out var previous);

                if (settled.Contains(current))
                {
                    continue;
                }

                settled.Add(current);

                if (!enumerator.MoveTo(current))
                {
                    continue;
                }

                while (enumerator.MoveNext())
                {
                    var n = enumerator.Head;
                    if (!enumerator.Forward) continue;

                    if (loop.Contains(n))
                    {
                        // yay, a loop!
                        loop.Add(current);
                        while (previous != uint.MaxValue)
                        {
                            pathTree.Get(previous, out current, out previous);
                            loop.Add(current);
                        }
                    }

                    if (settled.Contains(n))
                    {
                        continue;
                    }

                    queue.Enqueue(pathTree.Add(n, pointer));
                }
            }

            if (loop.Count > 1)
            {
                this.Merge(loop);
                hasMadeConnection = true;
            }

            loop.Clear();

            // move to the next label.
            label++;
        }

        return hasMadeConnection;
    }

    private void Merge(HashSet<uint> labels)
    {
        // build a list of neighbours of the labels to be removed.
        var edgeEnumerator = _labelGraph.GetEdgeEnumerator();
        var bestLabel = uint.MaxValue;
        var neighbours = new Dictionary<uint, (bool forward, bool backward)>();
        foreach (var label in labels)
        {
            if (label < bestLabel) bestLabel = label;
            if (!edgeEnumerator.MoveTo(label)) continue;

            while (edgeEnumerator.MoveNext())
            {
                var n = edgeEnumerator.Head;
                if (labels.Contains(n)) continue;

                (bool forward, bool backward) data = edgeEnumerator.Forward ? (true, false) : (false, true);

                if (neighbours.TryGetValue(n, out var existingData))
                {
                    data = (existingData.forward || data.forward, existingData.backward || data.backward);
                }

                neighbours[n] = data;
            }
        }

        // nothing was to be removed.
        if (bestLabel == uint.MaxValue) return;

        // add all edges to the neighbours again.
        foreach (var (neighbour, data) in neighbours)
        {
            if (data.forward)
            {
                if (!_labelGraph.HasEdge(bestLabel, neighbour)) _labelGraph.AddEdge(bestLabel, neighbour);
            }
            if (data.backward)
            {
                if (!_labelGraph.HasEdge(neighbour, bestLabel)) _labelGraph.AddEdge(neighbour, bestLabel);
            }
        }

        var bestData = _islands[bestLabel];
        foreach (var label in labels)
        {
            if (label == bestLabel) continue;
            _labelGraph.RemoveVertex(label);

            var data = _islands[label];
            bestData.size += data.size;
            _islands.Remove(label);
        }
        _islands[bestLabel] = bestData;

        foreach (var k in _labels.Keys)
        {
            var val = _labels[k];
            if (val == bestLabel) continue;
            if (!labels.Contains(val)) continue;

            _labels[k] = bestLabel;
        }
    }
}
