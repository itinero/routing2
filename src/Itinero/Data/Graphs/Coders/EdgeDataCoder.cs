using System.Collections.Generic;
using Itinero.Data.Graphs;

namespace Itinero.Data.Graphs.Coders
{
    /// <summary>
    /// An edge data encoder/decoder.
    /// </summary>
    /// <typeparam name="T">The input/output type.</typeparam>
    internal abstract class EdgeDataCoder<T>
    {
        /// <summary>
        /// Creates a new edge data coder. 
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="dataType">The data type.</param>
        protected EdgeDataCoder(int offset, EdgeDataType dataType)
        {
            this.Offset = offset;
            this.EdgeDataType = dataType;
        }
        
        /// <summary>
        /// Gets the offset.
        /// </summary>
        protected int Offset { get; }
        
        /// <summary>
        /// Gets the edge data type.
        /// </summary>
        public EdgeDataType EdgeDataType { get; }
        
        /// <summary>
        /// Gets decoded that for the given edge.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="value">The decoded data.</param>
        /// <returns>True when getting the value succeeds.</returns>
        public abstract bool Get(Graph.Enumerator enumerator, out T value);

        /// <summary>
        /// Sets the data on the given edge.
        /// </summary>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="value">The value to store.</param>
        /// <returns>True when setting the value succeeds.</returns>
        public abstract bool Set(Graph.Enumerator enumerator, T value);
    }
}