using System;
using System.Collections.Generic;
using System.IO;
using Itinero.IO;

namespace Itinero.Network.Indexes.EdgeTypes
{
    /// <summary>
    /// A graph type index.
    ///
    /// This has a:
    /// - A function to determine edge type.
    /// - A graph edge type collection.
    /// </summary>
    internal class EdgeTypeIndex
    {
        private readonly EdgeTypeFunc _func;
        private readonly EdgeTypeCollection _edgeTypes;

        public EdgeTypeIndex()
        {
            _func = new EdgeTypeFunc();
            _edgeTypes = new EdgeTypeCollection();
        }

        public int Id => _func.Id;

        public EdgeTypeFunc Func => _func;

        internal EdgeTypeCollection EdgeTypeCollection => _edgeTypes;

        private EdgeTypeIndex(EdgeTypeCollection edgeTypes, EdgeTypeFunc func)
        {
            _edgeTypes = edgeTypes;
            _func = func;
        }

        public uint Get(IEnumerable<(string key, string value)> attributes)
        {
            var edgeType = _func.ToEdgeType(attributes);
            return _edgeTypes.Get(edgeType);
        }

        public EdgeTypeIndex Next(EdgeTypeFunc func)
        {
            return new EdgeTypeIndex(_edgeTypes, func);
        }

        internal void Serialize(Stream stream)
        {
            // write version #.
            stream.WriteVarInt32(1);
            
            // write id.
            stream.WriteVarInt32(this.Id);
            
            // write edge types.
            _edgeTypes.Serialize(stream);
        }

        internal static EdgeTypeIndex Deserialize(Stream stream, 
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? func)
        {
            // get version #.
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Unexpected version #.");
            
            // read id.
            var id = stream.ReadVarInt32();
            EdgeTypeFunc edgeTypeFunc;
            if (id == 0)
            {
                // empty edge type function, it has id 0.
                edgeTypeFunc = new EdgeTypeFunc();
            }
            else
            {
                if (func == null)
                    throw new ArgumentNullException(nameof(func),
                        "Edge type function is null but it's ID indicates a function should be present.");
                edgeTypeFunc = new EdgeTypeFunc(id, func);
            }
            
            // read edge types.
            var edgeTypes = EdgeTypeCollection.Deserialize(stream);
            
            return new EdgeTypeIndex(edgeTypes, edgeTypeFunc);
        }
    }
}