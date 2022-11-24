using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Itinero.Network;
using Itinero.Network.Enumerators.Edges;

namespace Itinero.Routes.Paths;

/// <summary>
/// Represents a path in a graph as a collection of edges.
/// </summary>
public class Path : IEnumerable<(EdgeId edge, bool forward, ushort offset1, ushort offset2)>
{
    private readonly List<(EdgeId edge, bool forward)> _edges;
    public List<(EdgeId edge, bool forward)> Edges => _edges;
    private readonly RoutingNetworkEdgeEnumerator _edgeEnumerator;
    private readonly RoutingNetwork _network;

    public Path(RoutingNetwork network)
    {
        _network = network;
        _edgeEnumerator = network.GetEdgeEnumerator();

        _edges = new List<(EdgeId edge, bool forward)>();
    }

    internal Path(Path pathToClone)
    {
        _network = pathToClone._network;
        _edgeEnumerator = _network.GetEdgeEnumerator();

        _edges = new List<(EdgeId edge, bool forward)>(pathToClone._edges);

        this.Offset1 = pathToClone.Offset1;
        this.Offset2 = pathToClone.Offset2;
    }

    /// <summary>
    /// Gets the network.
    /// </summary>
    public RoutingNetwork RoutingNetwork => _network;

    /// <summary>
    /// Gets the offset at the start of the path.
    /// </summary>
    /// <remarks>
    /// This is independent of the the direction of the first edge:
    /// - 0   : means the edge is fully included.
    /// - max : means the edge is not included. 
    /// </remarks>
    public ushort Offset1 { get; set; } = 0;

    /// <summary>
    /// Gets the offset at the end of the path, relative to the direction of the edge.
    /// </summary>
    /// <remarks>
    /// This is independent of the the direction of the last edge:
    /// - 0   : means the edge is not included.
    /// - max : means the edge is fully included. 
    /// </remarks>
    public ushort Offset2 { get; set; } = ushort.MaxValue;

    /// <summary>
    /// Gets the first edge.
    /// </summary>
    public (EdgeId edge, bool direction) First => _edges[0];

    /// <summary>
    /// Gets the last edge.
    /// </summary>
    public (EdgeId edge, bool direction) Last => _edges[this.Count - 1];

    /// <summary>
    /// Remove the first edge.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void RemoveFirst()
    {
        if (_edges.Count == 0)
        {
            throw new InvalidOperationException("Cannot remove first from an already empty path.");
        }

        _edges.RemoveAt(0);
        this.Offset1 = 0;
    }

    /// <summary>
    /// Remove the last edge.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void RemoveLast()
    {
        if (_edges.Count == 0)
        {
            throw new InvalidOperationException("Cannot remove last from an already empty path.");
        }

        _edges.RemoveAt(_edges.Count - 1);
        this.Offset2 = ushort.MaxValue;
    }

    /// <summary>
    /// Returns the number of edges.
    /// </summary>
    public int Count => _edges.Count;

    /// <summary>
    /// Appends the given edge and calculates the proper direction.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="forward">The direction of the edge.</param>
    public void Append(EdgeId edge, bool forward)
    {
        if (!_edgeEnumerator.MoveTo(edge, forward)) throw new Exception($"Edge does not exist.");

#if DEBUG
        if (_edges.Count > 0)
        {
            var edgeEnumerator = _network.GetEdgeEnumerator();
            if (!edgeEnumerator.MoveTo(edge, forward)) throw new Exception($"Edge does not exist.");
            var tail = edgeEnumerator.Tail;
            var previous = _edges[^1];
            if (!edgeEnumerator.MoveTo(previous.edge, previous.forward)) throw new Exception($"Edge does not exist."); ;
            if (edgeEnumerator.Head != tail)
                throw new Exception("Cannot append edge, previous edge head does not match tail");
        }
#endif

        this.AppendInternal(edge, forward);
    }

    /// <summary>
    /// Prepends the given edge and calculates the proper direction.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <param name="forward">The direction of the edge.</param>
    public void Prepend(EdgeId edge, bool forward)
    {
        if (!_edgeEnumerator.MoveTo(edge, forward)) throw new Exception($"Edge does not exist.");

#if DEBUG
        if (_edges.Count > 0)
        {
            var edgeEnumerator = _network.GetEdgeEnumerator();
            if (!edgeEnumerator.MoveTo(edge, forward)) throw new Exception($"Edge does not exist.");
            var head = edgeEnumerator.Head;
            var next = _edges[0];
            if (!edgeEnumerator.MoveTo(next.edge, next.forward)) throw new Exception($"Edge does not exist.");
            if (edgeEnumerator.Tail != head)
                throw new Exception("Cannot append edge, next edge tail does not match head");
        }
#endif

        this.PrependInternal(edge, forward);
    }

    /// <summary>
    /// Returns a copy of this path.
    /// </summary>
    /// <returns>A copy.</returns>
    public Path Clone()
    {
        return new Path(this);
    }

    internal void AppendInternal(EdgeId edge, bool forward)
    {
        _edges.Add((edge, forward));
    }

    internal void PrependInternal(EdgeId edge, bool forward)
    {
        _edges.Insert(0, (edge, forward));
    }

    public IEnumerator<(EdgeId edge, bool forward, ushort offset1, ushort offset2)> GetEnumerator()
    {
        for (var i = 0; i < this.Count; i++)
        {
            var edge = _edges[i];
            var offset1 = (ushort)0;
            var offset2 = ushort.MaxValue;

            if (i == 0)
            {
                offset1 = this.Offset1;
            }

            if (i == this.Count - 1)
            {
                offset2 = this.Offset2;
            }

            yield return (edge.edge, edge.forward, offset1, offset2);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    /// <summary>
    /// Returns a description of this path.
    /// </summary>
    public override string ToString()
    {
        // 1 edge without offsets:         [0]->21543F->[4]
        // 1 edge with offsets:            [0]->10%-21543F-20%->[4]
        var builder = new StringBuilder();

        if (_edges.Count > 0)
        { // there is a first edge.
            var first = _edges[0];
            _edgeEnumerator.MoveTo(first.edge, first.forward);
            builder.Append($"[{_edgeEnumerator.Tail}]");
            builder.Append("->");
            if (this.Offset1 != 0)
            {
                builder.Append(OffsetPer(this.Offset1));
                builder.Append("-");
            }

            builder.Append($"{first.edge}");
            builder.Append(first.forward ? "F" : "B");

            if (_edges.Count == 1)
            {
                if ((first.forward && this.Offset2 != ushort.MaxValue) ||
                    (!first.forward && this.Offset2 != 0))
                {
                    builder.Append("-");
                    builder.Append(OffsetPer(this.Offset2));
                }

                builder.Append("->");
                builder.Append($"[{_edgeEnumerator.Head}]");
                return builder.ToString();
            }

            builder.Append("->");
            builder.Append($"[{_edgeEnumerator.Head}]");
        }

        for (var e = 1; e < _edges.Count - 1; e++)
        {
            var edgeAndDirection = _edges[e];
            _edgeEnumerator.MoveTo(edgeAndDirection.edge, edgeAndDirection.forward);
            builder.Append("->");
            builder.Append($"{edgeAndDirection.edge}");
            builder.Append(edgeAndDirection.forward ? "F" : "B");
            builder.Append("->");
            builder.Append($"[{_edgeEnumerator.Head}]");
        }

        if (_edges.Count > 0)
        { // there is a last edge.
            var last = _edges[^1];
            builder.Append("->");
            builder.Append($"{last.edge}");
            builder.Append(last.forward ? "F" : "B");
            if (this.Offset2 != ushort.MaxValue)
            {
                builder.Append("-");
                builder.Append(OffsetPer(this.Offset2));
            }

            _edgeEnumerator.MoveTo(last.edge, last.forward);
            builder.Append("->");
            builder.Append($"[{_edgeEnumerator.Head}]");
            return builder.ToString();
        }

        return builder.ToString();

        // Declare a local function.
        static string OffsetPer(ushort offset)
        {
            return $"{(double)offset / ushort.MaxValue * 100:F1}%";
        }
    }
}
