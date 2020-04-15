using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.DataStructures
{
    /// <summary>
    /// Represents a path in a graph as a collection of edges.
    /// </summary>
    public class Path : IReadOnlyList<(EdgeId edge, bool forward)>
    {
        private readonly List<(EdgeId edge, bool forward)> _edges;
        private readonly Graph.Enumerator _graphEnumerator;

        /// <summary>
        /// Creates a new empty path.
        /// </summary>
        /// <param name="graph">The graph.</param>
        public Path(Graph graph)
        {
            _graphEnumerator = graph.GetEnumerator();
            
            _edges = new List<(EdgeId edge, bool forward)>();
        }

        /// <summary>
        /// Gets the offset at the start of the path, relative to the direction of the edge.
        /// </summary>
        public ushort Offset1 { get; set; } = 0;

        /// <summary>
        /// Gets the offset at the end of the path, relative to the direction of the edge.
        /// </summary>
        public ushort Offset2 { get; set; } = 0;

        /// <summary>
        /// Gets the edge at the given index.
        /// </summary>
        /// <param name="i"></param>
        public (EdgeId edge, bool forward) this[int i] => _edges[i];

        /// <summary>
        /// Returns the number of edges.
        /// </summary>
        public int Count => _edges.Count;

        /// <summary>
        /// Appends the given edge and calculates the proper direction.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="first">The vertex that should occur first.</param>
        public void Append(EdgeId edge, VertexId first)
        {
            if (!_graphEnumerator.MoveToEdge(edge))
                throw new Exception($"Edge does not exist.");

            if (_graphEnumerator.From == first)
            {
                _edges.Insert(0, (edge, true));
            }
            else if (_graphEnumerator.To == first)
            {
                _edges.Insert(0, (edge, false));
            }
            else
            {
                throw new Exception($"Cannot append edge, the given vertex is not part of it."); 
            }
        }

        /// <summary>
        /// Prepends the given edge and calculates the proper direction.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="last">The vertex that should occur last.</param>
        public void Prepend(EdgeId edge, VertexId last)
        {
            if (!_graphEnumerator.MoveToEdge(edge)) 
                throw new Exception($"Edge does not exist.");

            if (_graphEnumerator.From == last)
            {
                _edges.Insert(0, (edge, false));
            }
            else if (_graphEnumerator.To == last)
            {
                _edges.Insert(0, (edge, true));
            }
            else
            {
                throw new Exception($"Cannot prepend edge, the given vertex is not part of it."); 
            }
        }

        /// <summary>
        /// Appends the given edge and calculates the proper direction.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="offset">The offset.</param>
        public void Append(EdgeId edge, ushort? offset = null)
        {
            if (_edges.Count == 0)
            {
                _edges.Add((edge, true));
                return;
            }

            if (!_graphEnumerator.MoveToEdge(_edges[0].edge))
                throw new Exception($"Edge in path does not exist.");

            var last = _edges[0].forward ? _graphEnumerator.To : _graphEnumerator.From;
            
            if (!_graphEnumerator.MoveToEdge(edge))
                throw new Exception($"Edge in path does not exist.");

            if (_graphEnumerator.From == last)
            {
                _edges.Insert(0, (edge, true));
            }
            else if (_graphEnumerator.To == last)
            {
                _edges.Insert(0, (edge, false));
            }
            else
            {
                throw new Exception($"Cannot append edge, the it has no vertex in common with the last edge."); 
            }
        }

        /// <summary>
        /// Prepends the given edge and calculates the proper direction.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="offset">The offset.</param>
        public void Prepend(EdgeId edge, ushort? offset = null)
        {
            if (_edges.Count == 0)
            {
                _edges.Add((edge, true));
                return;
            }

            if (!_graphEnumerator.MoveToEdge(_edges[_edges.Count - 1].edge))
                throw new Exception($"Edge in path does not exist.");

            var first = _edges[_edges.Count].forward ? _graphEnumerator.From : _graphEnumerator.To;
            
            if (!_graphEnumerator.MoveToEdge(edge))
                throw new Exception($"Edge in path does not exist.");

            if (_graphEnumerator.To == first)
            {
                _edges.Add((edge, true));
            }
            else if (_graphEnumerator.From == first)
            {
                _edges.Add((edge, false));
            }
            else
            {
                throw new Exception($"Cannot prepend edge, the it has no vertex in common with the first edge."); 
            }
        }

        /// <summary>
        /// Appends the given edge, without checking if the path is valid.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="forward">The forward flag.</param>
        internal void AppendInternal(EdgeId edge, bool forward)
        {
            _edges.Insert(0, (edge, forward));
        }

        /// <summary>
        /// Prepends the given edge, without checking if the path is valid.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="forward">The forward flag.</param>
        internal void PrependInternal(EdgeId edge, bool forward)
        {
            _edges.Add((edge, forward));
        }

        public IEnumerator<(EdgeId edge, bool forward)> GetEnumerator()
        {
            return _edges.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
                _graphEnumerator.MoveToEdge(first.edge, first.forward);
                builder.Append($"[{_graphEnumerator.From}]");
                builder.Append("->");
                if ((first.forward && this.Offset1 != 0) ||
                    (!first.forward && this.Offset1 != ushort.MaxValue))
                {
                    builder.Append(OffsetPer(this.Offset1, first.forward));
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
                        builder.Append(OffsetPer(this.Offset2, first.forward));
                    }
                    builder.Append("->");
                    builder.Append($"[{_graphEnumerator.To}]");
                    return builder.ToString();
                }
                builder.Append("->");
                builder.Append($"[{_graphEnumerator.To}]");
            }

            for (var e = 1; e < _edges.Count - 1; e++)
            {
                var edgeAndDirection = _edges[0];
                _graphEnumerator.MoveToEdge(edgeAndDirection.edge, edgeAndDirection.forward);
                builder.Append("->");
                builder.Append($"{edgeAndDirection.edge}");
                builder.Append(edgeAndDirection.forward ? "F" : "B");
                builder.Append("->");
                builder.Append($"[{_graphEnumerator.To}]");
            }

            if (_edges.Count > 0)
            { // there is a last edge.
                var last = _edges[_edges.Count - 1];
                if ((last.forward && this.Offset2 != ushort.MaxValue) ||
                    (!last.forward && this.Offset2 != 0))
                {
                    builder.Append("-");
                    builder.Append(OffsetPer(this.Offset2, last.forward));
                }
                builder.Append("->");
                builder.Append($"[{_graphEnumerator.To}]");
                return builder.ToString();
            }
            
            return builder.ToString();
            
            // Declare a local function.
            string OffsetPer(ushort offset, bool forward)
            {
                if (forward)
                {
                    return $"{(double) offset / ushort.MaxValue*100:F1}%";
                }
                return $"{(double) (ushort.MaxValue - offset) / ushort.MaxValue*100:F1}%";
            }
        }
    }
}