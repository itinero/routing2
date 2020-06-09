using System;
using System.Collections.Generic;
using System.IO;
using Itinero.IO;

namespace Itinero.Data.Graphs.EdgeTypes
{
    /// <summary>
    /// A graph type index.
    ///
    /// This has a:
    /// - A function to determine edge type.
    /// - A graph edge type collection.
    /// </summary>
    internal class GraphEdgeTypeIndex
    {
        private readonly GraphEdgeTypeFunc _func;
        private readonly GraphEdgeTypeCollection _edgeTypes;

        public GraphEdgeTypeIndex()
        {
            _func = new GraphEdgeTypeFunc();
            _edgeTypes = new GraphEdgeTypeCollection();
        }

        public int Id => _func.Id;

        public GraphEdgeTypeFunc Func => _func;

        internal GraphEdgeTypeCollection EdgeTypeCollection => _edgeTypes;

        private GraphEdgeTypeIndex(GraphEdgeTypeCollection edgeTypes, GraphEdgeTypeFunc func)
        {
            _edgeTypes = edgeTypes;
            _func = func;
        }

        public uint Get(IEnumerable<(string key, string value)> attributes)
        {
            var edgeType = _func.ToEdgeType(attributes);
            return _edgeTypes.Get(edgeType);
        }

        public IEnumerable<(string key, string value)> GetById(uint edgeTypeId)
        {
            return _edgeTypes.GetById(edgeTypeId);
        }

        public GraphEdgeTypeIndex Next(GraphEdgeTypeFunc func)
        {
            return new GraphEdgeTypeIndex(_edgeTypes, func);
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

        internal static GraphEdgeTypeIndex Deserialize(Stream stream, 
            Func<IEnumerable<(string key, string value)>, IEnumerable<(string key, string value)>>? func)
        {
            // get version #.
            var version = stream.ReadVarInt32();
            if (version != 1) throw new InvalidDataException("Unexpected version #.");
            
            // read id.
            var id = stream.ReadVarInt32();
            GraphEdgeTypeFunc edgeTypeFunc;
            if (id == 0)
            {
                // empty edge type function, it has id 0.
                edgeTypeFunc = new GraphEdgeTypeFunc();
            }
            else
            {
                if (func == null)
                    throw new ArgumentNullException(nameof(func),
                        "Edge type function is null but it's ID indicates a function should be present.");
                edgeTypeFunc = new GraphEdgeTypeFunc(id, func);
            }
            
            // read edge types.
            var edgeTypes = GraphEdgeTypeCollection.Deserialize(stream);
            
            return new GraphEdgeTypeIndex(edgeTypes, edgeTypeFunc);
        }
    }
}