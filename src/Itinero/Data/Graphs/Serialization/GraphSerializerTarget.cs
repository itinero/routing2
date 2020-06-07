using System;
using System.IO;

namespace Itinero.Data.Graphs.Serialization
{
    internal abstract class GraphSerializerTarget
    {
        /// <summary>
        /// Gets the stream to main graph stream.
        /// </summary>
        public abstract Stream GraphStream { get; }

        /// <summary>
        /// Gets a stream to write a specific tile.
        /// </summary>
        public abstract Stream GetTileStream((uint z, uint x, uint y) tile, Guid id);
    }
}