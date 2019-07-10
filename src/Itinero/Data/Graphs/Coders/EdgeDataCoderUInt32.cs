using System;

namespace Itinero.Data.Graphs.Coders
{
    /// <summary>
    /// An edge data coder with fixed edge data type.
    /// </summary>
    internal sealed class EdgeDataCoderUInt32 : EdgeDataCoder<uint>
    {
        /// <summary>
        /// Creates a new edge data coder.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public EdgeDataCoderUInt32(int offset)
            : base(offset, EdgeDataType.UInt32)
        {
            
        }

        /// <inheritdoc/>
        public override bool Get(Graph.Enumerator enumerator, out uint value)
        {
            var edges = enumerator.Edges;
            var pointer = enumerator.RawPointer + this.Offset + enumerator.EdgeSize - enumerator.Graph.EdgeDataSize;
            var bytes = new[] {edges[pointer + 0], edges[pointer + 1], edges[pointer + 2], edges[pointer + 3]}; // allocations to do this, nooo.

            value = default;
            if (bytes[0] == byte.MaxValue && bytes[1] == byte.MaxValue &&
                bytes[2] == byte.MaxValue && bytes[3] == byte.MaxValue) return false;
            value = BitConverter.ToUInt32(bytes, 0);
            return true;
        }

        /// <inheritdoc/>
        public override bool Set(Graph.Enumerator enumerator, uint value)
        {
            var edges = enumerator.Edges;
            var pointer = enumerator.RawPointer + this.Offset + enumerator.EdgeSize - enumerator.Graph.EdgeDataSize;
            var bytes = BitConverter.GetBytes(value);
            if (bytes[0] == byte.MaxValue && bytes[1] == byte.MaxValue &&
                bytes[2] == byte.MaxValue && bytes[3] == byte.MaxValue) return false;

            edges[pointer + 0] = bytes[0];
            edges[pointer + 1] = bytes[1];
            edges[pointer + 2] = bytes[2];
            edges[pointer + 3] = bytes[3];
            
            return true;
        }
    }
}