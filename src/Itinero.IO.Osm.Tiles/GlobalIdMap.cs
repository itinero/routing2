using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Itinero.Data.Graphs;

[assembly: InternalsVisibleTo("Itinero.Tests")]
[assembly: InternalsVisibleTo("Itinero.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Tests.Functional")]
namespace Itinero.IO.Osm.Tiles
{
    /// <summary>
    /// A data structure to keep mappings between global vertex ids and vertices.
    /// </summary>
    internal class GlobalIdMap : IEnumerable<(long globalId, VertexId vertex)>
    {
        private readonly Dictionary<long, VertexId> _vertexPerId = new Dictionary<long, VertexId>();

        /// <summary>
        /// Sets a new mapping.
        /// </summary>
        /// <param name="globalVertexId">The global vertex id.</param>
        /// <param name="vertex">The local vertex.</param>
        public void Set(long globalVertexId, VertexId vertex)
        {
            _vertexPerId[globalVertexId] = vertex;
        }

        /// <summary>
        /// Gets a mapping if it exists.
        /// </summary>
        /// <param name="globalVertexId">The global vertex id.</param>
        /// <param name="vertex">The vertex associated with the given global vertex, if any.</param>
        /// <returns>True if a mapping exists, false otherwise.</returns>
        public bool TryGet(long globalVertexId, out VertexId vertex)
        {
            return _vertexPerId.TryGetValue(globalVertexId, out vertex);
        }

        /// <inheritdoc/>
        public IEnumerator<(long globalId, VertexId vertex)> GetEnumerator()
        {
            foreach (var pair in _vertexPerId)
            {
                yield return (pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal long WriteTo(Stream stream)
        {
            var p = stream.Position;
            
            // write header and version.
            stream.WriteWithSize($"{nameof(GlobalIdMap)}");
            stream.WriteByte(1);
            
            // write data.
            stream.WriteInt64(_vertexPerId.Count);
            foreach (var pair in _vertexPerId)
            {
                stream.WriteInt64(pair.Key);
                stream.WriteVertexId(pair.Value);
            }

            return stream.Position - p;
        }

        internal static GlobalIdMap ReadFrom(Stream stream)
        {
            // read & verify header.
            var header = stream.ReadWithSizeString();
            var version = stream.ReadByte();
            if (header != nameof(GlobalIdMap)) throw new InvalidDataException($"Cannot read {nameof(GlobalIdMap)}: Header invalid.");
            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(GlobalIdMap)}: Version # invalid.");
            
            // read size first
            var globalIdMap = new GlobalIdMap();
            var size = stream.ReadInt64();
            for (var p = 0; p < size; p++)
            {
                var nodeId = stream.ReadInt64();
                var vertexId = stream.ReadVertexId();
                
                globalIdMap.Set(nodeId, vertexId);
            }

            return globalIdMap;
        }
    }
}