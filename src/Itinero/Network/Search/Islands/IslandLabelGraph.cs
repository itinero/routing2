using System;
using System.Collections.Generic;

namespace Itinero.Network.Search.Islands;

internal class IslandLabelGraph
{
    private const uint NoEdge = uint.MaxValue;
    private const uint NoVertex = uint.MaxValue - 1;

    private readonly List<uint> _vertices = new(); // Holds all vertices pointing to it's first edge.
    private readonly List<(uint vertex1, uint vertex2, uint pointer1, uint pointer2)> _edges = new();

    public uint AddVertex()
    {
        var vertex = (uint)_vertices.Count;
        _vertices.Add(NoEdge);
        return vertex;
    }

    public bool HasVertex(uint vertex)
    {
        if (vertex >= _vertices.Count) return false;
        if (_vertices[(int)vertex] == NoVertex) return false;

        return true;
    }

    public bool RemoveVertex(uint vertex)
    {
        if (!this.HasVertex(vertex)) return false;

        this.RemoveEdges(vertex);

        _vertices[(int)vertex] = NoVertex;

        while (_vertices.Count > 0 &&
            _vertices[^1] == NoVertex)
        {
            _vertices.RemoveAt(_vertices.Count - 1);
        }

        return true;
    }

    public void AddEdge(uint vertex1, uint vertex2)
    {
        if (vertex1 == vertex2) { throw new ArgumentException("Given vertices must be different."); }
        if (!this.HasVertex(vertex1)) throw new ArgumentException($"Vertex {vertex1} does not exist.");
        if (!this.HasVertex(vertex2)) throw new ArgumentException($"Vertex {vertex2} does not exist.");

        this.AddEdgeInternal(vertex1, vertex2);
    }

    public bool HasEdge(uint vertex1, uint vertex2)
    {
        if (vertex1 == vertex2) { throw new ArgumentException("Given vertices must be different."); }
        if (!this.HasVertex(vertex1)) throw new ArgumentException($"Vertex {vertex1} does not exist.");
        if (!this.HasVertex(vertex2)) throw new ArgumentException($"Vertex {vertex2} does not exist.");

        var edgeId = _vertices[(int)vertex1];
        while (edgeId != NoEdge)
        {
            // get the edge.
            var edge = _edges[(int)edgeId];
            if (edge.vertex1 == vertex1)
            {
                if (edge.vertex2 == vertex2)
                {
                    return true;
                }
                edgeId = edge.pointer1;
            }
            else if (edge.vertex2 == vertex1)
            {
                edgeId = edge.pointer2;
            }
            else
            {
                throw new Exception("Edge found on vertex set that does not contain vertex");
            }
        }

        return false;
    }

    public int RemoveEdges(uint vertex)
    {
        var removed = 0;
        var edges = this.GetEdgeEnumerator();
        while (edges.MoveTo(vertex) && edges.MoveNext())
        {
            if (edges.Forward)
            {
                this.RemoveEdge(vertex, edges.Head);
            }
            else
            {
                this.RemoveEdge(edges.Head, vertex);
            }
            removed++;
        }

        return removed;
    }

    public bool RemoveEdge(uint vertex1, uint vertex2)
    {
        if (vertex1 == vertex2) throw new ArgumentException("Given vertices must be different.");
        if (!this.HasVertex(vertex1)) throw new ArgumentException($"Vertex {vertex1} does not exist.");
        if (!this.HasVertex(vertex2)) throw new ArgumentException($"Vertex {vertex2} does not exist.");

        var edgeId = this.RemoveEdgeFromVertex1(vertex1, vertex2);
        if (edgeId == NoEdge) return false;

        this.RemoveEdgeFromVertex(vertex2, edgeId);

        _edges[(int)edgeId] = (NoVertex, NoVertex, NoEdge, NoEdge);

        while (_edges.Count > 0 && _edges[^1].vertex1 == NoVertex)
        {
            _edges.RemoveAt(_edges.Count - 1);
        }
        return true;
    }

    private uint RemoveEdgeFromVertex1(uint vertex1, uint vertex2)
    {
        // find the edge and keep the previous edge id.
        var edgeId = _vertices[(int)vertex1];
        var previousEdgeId = NoEdge;
        var foundEdgeId = NoEdge;
        while (edgeId != NoEdge)
        {
            // get the edge.
            var edge = _edges[(int)edgeId];
            if (edge.vertex1 == vertex1)
            {
                if (edge.vertex2 == vertex2)
                {
                    foundEdgeId = edgeId;
                    edgeId = edge.pointer1;
                    break;
                }
                previousEdgeId = edgeId;
                edgeId = edge.pointer1;
            }
            else if (edge.vertex2 == vertex1)
            {
                previousEdgeId = edgeId;
                edgeId = edge.pointer2;
            }
            else
            {
                throw new Exception("Edge found on vertex set that does not contain vertex");
            }
        }

        if (foundEdgeId == NoEdge) return NoEdge;

        // set pointer on last edge or vertex.
        if (previousEdgeId == NoEdge)
        {
            _vertices[(int)vertex1] = edgeId;
        }
        else
        {
            var edge = _edges[(int)previousEdgeId];
            if (edge.vertex1 == vertex1)
            {
                edge.pointer1 = edgeId;
            }
            else if (edge.vertex2 == vertex1)
            {
                edge.pointer2 = edgeId;
            }
            _edges[(int)previousEdgeId] = edge;
        }

        return foundEdgeId;
    }

    private void RemoveEdgeFromVertex(uint vertex, uint edgeIdToRemove)
    {
        if (edgeIdToRemove == NoEdge) throw new Exception("This edge has to exist, it existing in other vertex");

        var edgeId = _vertices[(int)vertex];
        var previousEdgeId = NoEdge;
        while (edgeId != NoEdge)
        {
            // get the edge.
            var edge = _edges[(int)edgeId];
            var currentEdgeId = edgeId;

            if (edge.vertex1 == vertex)
            {
                edgeId = edge.pointer1;
            }
            else if (edge.vertex2 == vertex)
            {
                edgeId = edge.pointer2;
            }
            else
            {
                throw new Exception("Edge found on vertex set that does not contain vertex");
            }

            if (currentEdgeId == edgeIdToRemove) break;
            previousEdgeId = currentEdgeId;
        }

        if (previousEdgeId == NoEdge)
        {
            _vertices[(int)vertex] = edgeId;
        }
        else
        {
            var edge = _edges[(int)previousEdgeId];
            if (edge.vertex1 == vertex)
            {
                edge.pointer1 = edgeId;
            }
            else if (edge.vertex2 == vertex)
            {
                edge.pointer2 = edgeId;
            }
            _edges[(int)previousEdgeId] = edge;
        }
    }

    public bool HasEdge(uint vertex1)
    {
        var enumerator = this.GetEdgeEnumerator();
        if (!enumerator.MoveTo(vertex1)) return false;

        return enumerator.MoveNext();
    }

    public EdgeEnumerator GetEdgeEnumerator()
    {
        return new EdgeEnumerator(this);
    }

    /// <summary>
    /// Returns the number of vertices in this graph.
    /// </summary>
    public int VertexCount => _vertices.Count;

    /// <summary>
    /// Returns the number of edges in this graph.
    /// </summary>
    public long EdgeCount => _edges.Count;

    private uint AddEdgeInternal(uint vertex1, uint vertex2)
    {
        // this adds an edge without checking for duplicates!
        var vertex1EdgeId = _vertices[(int)vertex1];
        var vertex2EdgeId = _vertices[(int)vertex2];

        var newEdgeId = (uint)_edges.Count;
        _vertices[(int)vertex1] = newEdgeId;
        _vertices[(int)vertex2] = newEdgeId;

        _edges.Add((vertex1, vertex2, vertex1EdgeId, vertex2EdgeId));

        return newEdgeId;
    }

    /// <summary>
    /// An edge enumerator.
    /// </summary>
    public class EdgeEnumerator
    {
        private readonly IslandLabelGraph _islandLabelGraph;
        private uint _nextEdgeId = NoEdge;

        /// <summary>
        /// Creates a new edge enumerator.
        /// </summary>
        internal EdgeEnumerator(IslandLabelGraph islandLabelGraph)
        {
            _islandLabelGraph = islandLabelGraph;
        }

        /// <summary>
        /// Move to the next edge.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (this.Tail == NoVertex) return false;
            if (_nextEdgeId == NoEdge) return false;

            var edge = _islandLabelGraph._edges[(int)_nextEdgeId];
            if (edge.vertex1 == this.Tail)
            {
                this.Head = edge.vertex2;
                _nextEdgeId = edge.pointer1;
                this.Forward = true;
                return true;
            }
            if (edge.vertex2 == this.Tail)
            {
                this.Head = edge.vertex1;
                _nextEdgeId = edge.pointer2;
                this.Forward = false;
                return true;
            }

            throw new Exception("Next edge does not have vertex");
        }

        public uint Tail { get; private set; } = NoVertex;

        public uint Head { get; private set; } = NoVertex;

        public bool Forward { get; private set; } = true;

        public bool MoveTo(uint vertex)
        {
            if (!_islandLabelGraph.HasVertex(vertex)) return false;

            _nextEdgeId = _islandLabelGraph._vertices[(int)vertex];
            if (_nextEdgeId == NoVertex) return false;

            this.Tail = vertex;
            return true;
        }

        public void Reset()
        {
            if (this.Tail == NoVertex) return;

            this.MoveTo(this.Tail);
        }
    }
}
