using System;

namespace Itinero.Data.Graphs.Serialization
{
    internal class GraphSerializer : IDisposable
    {
        private readonly Graph.MutableGraph _mutableGraph;

        public GraphSerializer(Graph.MutableGraph mutableGraph)
        {
            _mutableGraph = mutableGraph;
        }

        public void Serialize(GraphSerializerSettings settings)
        {
            
        }

        public void Dispose()
        {
            _mutableGraph.Dispose();
        }
    }
}