using System;

namespace Itinero.Data.Graphs
{
    internal class GraphWriter : IDisposable
    {
        private readonly Graph _graph;

        public GraphWriter(Graph graph)
        {
            _graph = graph;
        }

        public void Dispose()
        {
            
        }
    }
}