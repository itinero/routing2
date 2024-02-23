using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Routing.DataStructures;

namespace Itinero.Network.Search.Islands;

internal class IslandLabels
{
    /// <summary>
    /// The not an island label.
    /// </summary>
    public const uint NotAnIslandLabel = 0;

    /// <summary>
    /// Keeps a label for each edge.
    /// - Starting each edge starts with it's own unique Id.
    /// - When edges are bidirectionally connected they get take on the lowest Id of their neighbour.
    /// </summary>
    private readonly Dictionary<EdgeId, uint> _labels = new();
    private readonly Dictionary<uint, (uint size, bool final)> _islands = new(); // holds the size per island.
    private readonly IslandLabelGraph _labelGraph = new();
    private readonly int _maxIslandSize;

    internal IslandLabels(int maxIslandSize)
    {
        _maxIslandSize = maxIslandSize;
        _islands.Add(0, (uint.MaxValue, true));
        _labelGraph.AddVertex();
    }

    /// <summary>
    /// Gets all the islands.
    /// </summary>
    public IEnumerable<(uint label, uint size, bool final)> Islands
    {
        get
        {
            foreach (var (label, (size, final)) in _islands)
            {
                yield return (label, size, final);
            }
        }
    }

    /// <summary>
    /// Creates a new label.
    /// </summary>
    /// <returns></returns>
    public (uint label, uint size, bool final) AddNew(EdgeId edge, bool final = false)
    {
        if (_labels.ContainsKey(edge)) throw new ArgumentOutOfRangeException(nameof(edge));

        var label = _labelGraph.AddVertex();

        _labels[edge] = label;
        _islands[label] = (1, final);

        return (label, 1, final);
    }

    /// <summary>
    /// Adds an edge to an already existing island.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="edge"></param>
    /// <returns></returns>
    public (uint label, uint size, bool final) AddTo(uint label, EdgeId edge)
    {
        if (!_islands.TryGetValue(label, out var islandDetails)) throw new ArgumentOutOfRangeException(nameof(label));

        if (islandDetails.size < uint.MaxValue) islandDetails.size += 1;
        _islands[label] = (islandDetails.size, final: islandDetails.final);
        _labels[edge] = label;

        if (_islands[label].size >= _maxIslandSize &&
            label != NotAnIslandLabel)
        {
            // this island just grew over the maximum.
            this.Merge([label, NotAnIslandLabel]);
            var labelDetails = _islands[NotAnIslandLabel];
            return (NotAnIslandLabel, labelDetails.size, final: islandDetails.final);
        }
        else
        {
            var labelDetails = _islands[label];
            return (label, labelDetails.size, final: islandDetails.final);
        }
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

        if (headHasEdge) return this.FindLoops(head);
        return false;
    }

    /// <summary>
    /// Merges the two given islands.
    /// </summary>
    /// <param name="tail"></param>
    /// <param name="head"></param>
    internal void Merge(uint tail, uint head)
    {
        if (tail == head) return;

        if (!_labelGraph.HasVertex(tail)) throw new ArgumentException("Label does not exist", nameof(tail));
        if (!_labelGraph.HasVertex(head)) throw new ArgumentException("Label does not exist", nameof(head));

        this.Merge([tail, head]);

        // var edgesAdded = false;
        // if (!_labelGraph.HasEdge(tail, head))
        // {
        //     edgesAdded = true;
        //     _labelGraph.AddEdge(tail, head);
        // }
        // if (!_labelGraph.HasEdge(head, tail))
        // {
        //     edgesAdded = true;
        //     _labelGraph.AddEdge(head, tail);
        // }
        // if (!edgesAdded) return;
        //
        // this.FindLoops(head);
    }

    /// <summary>
    /// Gets the label for the given edge, if any.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="label"></param>
    /// <returns></returns>
    public bool TryGet(EdgeId edge, out uint label)
    {
        return _labels.TryGetValue(edge, out label);
    }

    /// <summary>
    /// Gets the island label for the given edge and its size, if any.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="island"></param>
    /// <returns></returns>
    public bool TryGetWithDetails(EdgeId edge, out (uint label, uint size, bool final) island)
    {
        island = (0, 0, false);
        if (!_labels.TryGetValue(edge, out var label)) return false;

        if (!_islands.TryGetValue(label, out var islandState))
            throw new Exception("Island does not exist");

        island = (label, islandState.size, final: islandState.final);
        return true;
    }

    /// <summary>
    /// Sets the given island as final, it cannot grow any further.
    /// </summary>
    /// <param name="label">The label of the island.</param>
    public void SetAsFinal(uint label)
    {
        if (!_islands.TryGetValue(label, out var islandState))
            throw new Exception("Island does not exist");

        _islands[label] = (islandState.size, true);
    }

    private bool FindLoops(uint label)
    {
        var pathTree = new PathTree();
        var settled = new HashSet<uint>();
        var queue = new Queue<uint>();
        var loop = new HashSet<uint>(); // keeps all with a path back to label, initially only label.

        var loopFound = false;
        var enumerator = _labelGraph.GetEdgeEnumerator();
        while (true)
        {
            if (!enumerator.MoveTo(label)) throw new Exception("Cannot search for loops on label that does not exist");

            loop.Add(label);
            queue.Enqueue(pathTree.Add(label, uint.MaxValue));

            while (queue.Count > 0)
            {
                var pointer = queue.Dequeue();
                pathTree.Get(pointer, out var current, out var previous);

                if (!settled.Add(current)) continue;
                if (!enumerator.MoveTo(current)) continue;

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

                    if (settled.Contains(n)) continue;
                    queue.Enqueue(pathTree.Add(n, pointer));
                }
            }

            if (loop.Count <= 1) return loopFound;

            label = this.Merge(loop);
            loopFound = true;

            loop.Clear();
            pathTree.Clear();
            settled.Clear();
            queue.Clear();
        }
    }

    private uint Merge(HashSet<uint> labels)
    {
        while (true)
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
            if (bestLabel == uint.MaxValue) throw new Exception("Cannot merge empty loop");

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
                if (bestData.size < uint.MaxValue) bestData.size += data.size;
                _islands.Remove(label);
            }

            _islands[bestLabel] = bestData;

            foreach (var k in _labels.Keys.ToArray())
            {
                var val = _labels[k];
                if (val == bestLabel) continue;
                if (!labels.Contains(val)) continue;

                _labels[k] = bestLabel;
            }

            if (bestLabel == NotAnIslandLabel) return bestLabel;
            if (_islands[bestLabel].size < _maxIslandSize)
            {
                return bestLabel;
            }

            labels = [bestLabel, NotAnIslandLabel];
            continue;
        }
    }
}
