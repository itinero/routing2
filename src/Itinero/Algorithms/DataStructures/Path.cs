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
    public class Path : IEnumerable<(EdgeId edge, bool forward, ushort offset1, ushort offset2)>
    {
        private readonly List<(EdgeId edge, bool forward)> _edges;
        private readonly NetworkEdgeEnumerator _edgeEnumerator;

        /// <summary>
        /// Creates a new empty path.
        /// </summary>
        /// <param name="network">The network.</param>
        public Path(Network network)
        {
            _edgeEnumerator = network.GetEdgeEnumerator();
            
            _edges = new List<(EdgeId edge, bool forward)>();
        }

        /// <summary>
        /// Gets the router db.
        /// </summary>
        public Network RouterDb => _edgeEnumerator.Network;

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

        internal void RemoveFirst()
        {
            if (_edges.Count == 0) throw new InvalidOperationException("Cannot remove first from an already empty path.");
            _edges.RemoveAt(0);
        }

        internal void RemoveLast()
        {
            if (_edges.Count == 0) throw new InvalidOperationException("Cannot remove last from an already empty path.");
            _edges.RemoveAt(_edges.Count - 1);
        }

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
            if (!_edgeEnumerator.MoveToEdge(edge))
                throw new Exception($"Edge does not exist.");

            if (_edgeEnumerator.From == first)
            {
                this.AppendInternal(edge, true);
            }
            else if (_edgeEnumerator.To == first)
            {
                this.AppendInternal(edge, false);
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
            if (!_edgeEnumerator.MoveToEdge(edge)) 
                throw new Exception($"Edge does not exist.");

            if (_edgeEnumerator.From == last)
            {
                this.PrependInternal(edge, false);
            }
            else if (_edgeEnumerator.To == last)
            {
                this.PrependInternal(edge, true);
            }
            else
            {
                throw new Exception($"Cannot prepend edge, the given vertex is not part of it."); 
            }
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
                
                if (i == 0) offset1 = this.Offset1;
                if (i == this.Count - 1) offset2 = this.Offset2;

                yield return (edge.edge, edge.forward, offset1, offset2);
            }
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
                _edgeEnumerator.MoveToEdge(first.edge, first.forward);
                builder.Append($"[{_edgeEnumerator.From}]");
                builder.Append("->");
                if (this.Offset1 != 0)
                {
                    builder.Append(OffsetPer(this.Offset1, true));
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
                    builder.Append($"[{_edgeEnumerator.To}]");
                    return builder.ToString();
                }
                builder.Append("->");
                builder.Append($"[{_edgeEnumerator.To}]");
            }

            for (var e = 1; e < _edges.Count - 1; e++)
            {
                var edgeAndDirection = _edges[e];
                _edgeEnumerator.MoveToEdge(edgeAndDirection.edge, edgeAndDirection.forward);
                builder.Append("->");
                builder.Append($"{edgeAndDirection.edge}");
                builder.Append(edgeAndDirection.forward ? "F" : "B");
                builder.Append("->");
                builder.Append($"[{_edgeEnumerator.To}]");
            }

            if (_edges.Count > 0)
            { // there is a last edge.
                var last = _edges[_edges.Count - 1];
                builder.Append("->");
                builder.Append($"{last.edge}");
                builder.Append(last.forward ? "F" : "B");
                if (this.Offset2 != ushort.MaxValue)
                {
                    builder.Append("-");
                    builder.Append(OffsetPer(this.Offset2, true));
                }
                _edgeEnumerator.MoveToEdge(last.edge, last.forward);
                builder.Append("->");
                builder.Append($"[{_edgeEnumerator.To}]");
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