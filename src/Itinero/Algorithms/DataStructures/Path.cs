using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Data.Graphs;

namespace Itinero.Algorithms.DataStructures
{
    /// <summary>
    /// Represents a path in a graph.
    /// </summary>
    public class Path
    {
        private readonly List<(uint edge, bool forward)> _edges;
        private readonly Graph _graph;
        private readonly Graph.Enumerator _graphEnumerator;
        private readonly ushort _offset1 = 0;
        private readonly ushort _offset2 = ushort.MaxValue;

        /// <summary>
        /// Creates a new empty path.
        /// </summary>
        /// <param name="graph">The graph.</param>
        public Path(Graph graph)
        {
            _graph = graph;
            _graphEnumerator = _graph.GetEnumerator();
            
            _edges = new List<(uint edge, bool forward)>();
        }

        /// <summary>
        /// Appends the given edge and calculates the proper direction.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="first">The vertex that should occur first.</param>
        public void Append(uint edge, VertexId first)
        {
            if (!_graphEnumerator.MoveToEdge(edge))
            {
                throw new Exception($"Edge does not exist.");
            }

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
        public void Prepend(uint edge, VertexId last)
        {
            if (!_graphEnumerator.MoveToEdge(edge))
            {
                throw new Exception($"Edge does not exist.");
            }

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
        public void Append(uint edge)
        {
            if (_edges.Count == 0)
            {
                _edges.Add((edge, true));
            }

            if (!_graphEnumerator.MoveToEdge(_edges[0].edge))
            {
                throw new Exception($"Edge in path does not exist.");
            }

            var last = _edges[0].forward ? _graphEnumerator.To : _graphEnumerator.From;
            
            if (!_graphEnumerator.MoveToEdge(edge))
            {
                throw new Exception($"Edge in path does not exist.");
            }

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
        public void Prepend(uint edge)
        {
            if (_edges.Count == 0)
            {
                _edges.Add((edge, true));
            }

            if (!_graphEnumerator.MoveToEdge(_edges[_edges.Count - 1].edge))
            {
                throw new Exception($"Edge in path does not exist.");
            }

            var first = _edges[_edges.Count].forward ? _graphEnumerator.From : _graphEnumerator.To;
            
            if (!_graphEnumerator.MoveToEdge(edge))
            {
                throw new Exception($"Edge in path does not exist.");
            }

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
        internal void AppendInternal(uint edge, bool forward)
        {
            _edges.Insert(0, (edge, forward));
        }

        /// <summary>
        /// Prepends the given edge, without checking if the path is valid.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="forward">The forward flag.</param>
        internal void PrependInternal(uint edge, bool forward)
        {
            _edges.Add((edge, forward));
        }
    }
}