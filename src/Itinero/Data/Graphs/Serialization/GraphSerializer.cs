using System;
using Itinero.IO;

namespace Itinero.Data.Graphs.Serialization
{
    internal class GraphSerializer : IDisposable
    {
        private readonly Graph.MutableGraph _mutableGraph;

        public GraphSerializer(Graph.MutableGraph mutableGraph)
        {
            _mutableGraph = mutableGraph;
        }

        public void Serialize(GraphSerializerTarget target)
        {
            // write version #.
            target.GraphStream.WriteVarInt32(1);
            
            // write edge types.
            
        }

        public void Dispose()
        {
            _mutableGraph.Dispose();
        }
    }
}