using System;

namespace Itinero.Data.Graphs
{
    /// <summary>
    /// Represents a graph data profile.
    /// </summary>
    public abstract class GraphDataProfileBase<T>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the size in bytes.
        /// </summary>
        public int Size { get; set; }
        
        /// <summary>
        /// Gets or sets the flag to indicate if this is inline or not.
        /// </summary>
        public bool Inline { get; set; }

        /// <summary>
        /// Gets the data from the given graph.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="edge">The edge.</param>
        /// <returns>The data.</returns>
        public abstract T Get(Graph graph, ulong edge);

        /// <summary>
        /// Sets the data on the given graph.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="edge">The edge.</param>
        /// <param name="value">The value</param>
        public abstract void Set(Graph graph, ulong edge, T value);
    }
}