using System;
using System.Collections.Generic;
using System.Text;

namespace Itinero.Tiled.Algorithms
{
    /// <summary>
    /// Represents a path along a set of edges/vertices.
    /// </summary>
    public class TiledEdgePath<T>
    {
        /// <summary>
        /// Creates a path source.
        /// </summary>
        public TiledEdgePath(ulong vertex = Constants.NO_VERTEX)
        {
            this.Vertex = vertex;
            this.Edge = Constants.NO_EDGE;
            this.Weight = default(T);
            this.From = null;
        }

        /// <summary>
        /// Creates a path to the given vertex with the given weight.
        /// </summary>
        public TiledEdgePath(ulong vertex, T weight, TiledEdgePath<T> from)
        {
            this.Vertex = vertex;
            this.Edge = Constants.NO_EDGE;
            this.Weight = weight;
            this.From = from;
        }

        /// <summary>
        /// Creates a path to the given vertex with the given weight along the given edge.
        /// </summary>
        public TiledEdgePath(ulong vertex, T weight, long edge, TiledEdgePath<T> from)
        {
            this.Vertex = vertex;
            this.Edge = edge;
            this.Weight = weight;
            this.From = from;
        }

        /// <summary>
        /// Gets the edge right before the vertex.
        /// </summary>
        public long Edge { get; set; }

        /// <summary>
        /// Gets the vertex.
        /// </summary>
        public ulong Vertex { get; set; }

        /// <summary>
        /// Gets the weight at the vertex.
        /// </summary>
        public T Weight { get; set; }

        /// <summary>
        /// Gets previous path.
        /// </summary>
        public TiledEdgePath<T> From { get; set; }

        /// <summary>
        /// Returns a description of this path.
        /// </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            var next = this;
            while (next != null)
            {
                if (next.From != null)
                {
                    builder.Insert(0, string.Format("->{2}->{0}[{1}]", next.Vertex, next.Weight, next.Edge));
                }
                else
                {
                    builder.Insert(0, string.Format("{0}[{1}]", next.Vertex, next.Weight));
                }
                next = next.From;
            }
            return builder.ToString();
        }

        /// <summary>
        /// Returns true if the given object represents the same edge/vertex.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as TiledEdgePath<T>;
            if (other == null)
            {
                return false;
            }
            return other.Edge == this.Edge &&
                other.Vertex == this.Vertex;
        }

        /// <summary>
        /// Serves as a hashfunction for this type.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Edge.GetHashCode() ^
                this.Vertex.GetHashCode();
        }
    }
}