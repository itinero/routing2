using System;
using System.Collections.Generic;

namespace Itinero.MapMatching.Model;

public class GraphModel
{
    private readonly List<GraphNode> _nodes = new();
    private readonly Dictionary<int, List<GraphEdge>> _edges = new();

    public GraphModel(Track track)
    {
        this.Track = track;
    }

    public Track Track { get; }

    public int Count => _nodes.Count;

    public int? FirstNode { get; private set; }

    public int? LastNode { get; private set; }

    public int AddNode(GraphNode node)
    {
        var id = _nodes.Count;
        _nodes.Add(node);

        this.FirstNode = this.FirstNode ?? id;
        this.LastNode = id;
        return id;
    }

    public void AddEdge(GraphEdge edge)
    {
        if (!_edges.TryGetValue(edge.Node1, out var edges))
        {
            edges = new List<GraphEdge>();
            _edges.Add(edge.Node1, edges);
        }
        edges.Add(edge);
    }

    public GraphNode GetNode(int node)
    {
        return _nodes[node];
    }

    public IEnumerable<GraphEdge> GetNeighbours(int node)
    {
        if (!_edges.TryGetValue(node, out var neighbours))
        {
            return ArraySegment<GraphEdge>.Empty;
        }

        return neighbours;
    }
}
