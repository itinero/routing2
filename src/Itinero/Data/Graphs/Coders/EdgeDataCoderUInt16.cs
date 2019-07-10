using System;

namespace Itinero.Data.Graphs.Coders
{
    /// <summary>
    /// An edge data coder with fixed edge data type.
    /// </summary>
    internal sealed class EdgeDataCoderUInt16 : EdgeDataCoder<ushort>
    {
        /// <summary>
        /// Creates a new edge data coder.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public EdgeDataCoderUInt16(int offset)
            : base(offset, EdgeDataType.UInt16)
        {
            
        }

        /// <inheritdoc/>
        public override bool Get(Graph.Enumerator enumerator, out ushort value)
        {
            var edges = enumerator.Edges;
            var pointer = enumerator.RawPointer + this.Offset + enumerator.EdgeSize;
            var bytes = new[] {edges[pointer + 0], edges[pointer + 1]}; // allocations to do this, nooo.

            value = default;
            if (bytes[0] == byte.MaxValue && bytes[1] == byte.MaxValue) return false;
            value = BitConverter.ToUInt16(bytes, 0);
            return true;
        }

        /// <inheritdoc/>
        public override bool Set(Graph.Enumerator enumerator, ushort value)
        {
            var edges = enumerator.Edges;
            var pointer = enumerator.RawPointer + this.Offset + enumerator.EdgeSize;
            var bytes = BitConverter.GetBytes(value);
            if (bytes[0] == byte.MaxValue && bytes[1] == byte.MaxValue) return false;

            edges[pointer + 0] = bytes[0];
            edges[pointer + 1] = bytes[1];
            
            return true;
        }
    }
}